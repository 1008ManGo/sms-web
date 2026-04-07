using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmppClient.Utils;
using SmppGateway.Models;
using SmppGateway.Models.Admin;
using SmppGateway.Services;
using SmppStorage.Entities;
using SmppStorage.Repositories;

namespace SmppGateway.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(AuthenticationSchemes = "AdminKey")]
public class AdminController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IBillingService _billingService;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IUserRepository userRepository,
        IPermissionRepository permissionRepository,
        IAccountRepository accountRepository,
        IBillingService billingService,
        IAuditLogRepository auditLogRepository,
        ILogger<AdminController> logger)
    {
        _userRepository = userRepository;
        _permissionRepository = permissionRepository;
        _accountRepository = accountRepository;
        _billingService = billingService;
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    [HttpPost("users")]
    public async Task<ApiResponse<UserDetailResponse>> CreateUser([FromBody] CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return ApiResponse<UserDetailResponse>.Fail(1, "Username and password are required");
        }

        if (await _userRepository.GetByUsernameAsync(request.Username) != null)
        {
            return ApiResponse<UserDetailResponse>.Fail(1, "Username already exists");
        }

        var user = new UserEntity
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            ApiKey = GenerateApiKey(request.Username),
            PasswordHash = HashPassword(request.Password),
            Balance = request.InitialBalance,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.CreateAsync(user);

        _logger.LogInformation("Admin created user: {Username}", request.Username);

        return ApiResponse<UserDetailResponse>.Success(MapToUserDetail(user));
    }

    [HttpGet("users")]
    public async Task<ApiResponse<List<UserDetailResponse>>> GetUsers()
    {
        var users = await _userRepository.GetAllAsync();
        var result = new List<UserDetailResponse>();

        foreach (var user in users)
        {
            result.Add(await MapToUserDetailAsync(user.Id));
        }

        return ApiResponse<List<UserDetailResponse>>.Success(result);
    }

    [HttpGet("users/{userId}")]
    public async Task<ApiResponse<UserDetailResponse>> GetUser(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return ApiResponse<UserDetailResponse>.Fail(1, "User not found");
        }

        return ApiResponse<UserDetailResponse>.Success(await MapToUserDetailAsync(userId));
    }

    [HttpPost("users/{userId}/countries")]
    public async Task<ApiResponse> AssignCountries(Guid userId, [FromBody] AssignCountriesRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return ApiResponse.Fail(1, "User not found");
        }

        foreach (var countryCode in request.CountryCodes)
        {
            await _permissionRepository.AddCountryPermissionAsync(userId, countryCode);
        }

        _logger.LogInformation("Admin assigned countries {Countries} to user {UserId}", 
            string.Join(",", request.CountryCodes), userId);

        return ApiResponse.Success("Countries assigned");
    }

    [HttpDelete("users/{userId}/countries/{countryCode}")]
    public async Task<ApiResponse> RemoveCountry(Guid userId, string countryCode)
    {
        await _permissionRepository.RemoveCountryPermissionAsync(userId, countryCode);
        _logger.LogInformation("Admin removed country {Country} from user {UserId}", countryCode, userId);
        return ApiResponse.Success("Country removed");
    }

    [HttpGet("users/{userId}/countries")]
    public async Task<ApiResponse<List<string>>> GetUserCountries(Guid userId)
    {
        var countries = await _permissionRepository.GetAllowedCountriesAsync(userId);
        return ApiResponse<List<string>>.Success(countries.ToList());
    }

    [HttpPost("users/{userId}/channels")]
    public async Task<ApiResponse> AssignChannels(Guid userId, [FromBody] AssignChannelsRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return ApiResponse.Fail(1, "User not found");
        }

        foreach (var channel in request.Channels)
        {
            await _permissionRepository.AddChannelPermissionAsync(userId, channel.AccountId, channel.MaxTps);
        }

        _logger.LogInformation("Admin assigned channels to user {UserId}", userId);
        return ApiResponse.Success("Channels assigned");
    }

    [HttpDelete("users/{userId}/channels/{accountId}")]
    public async Task<ApiResponse> RemoveChannel(Guid userId, string accountId)
    {
        await _permissionRepository.RemoveChannelPermissionAsync(userId, accountId);
        _logger.LogInformation("Admin removed channel {Channel} from user {UserId}", accountId, userId);
        return ApiResponse.Success("Channel removed");
    }

    [HttpGet("users/{userId}/channels")]
    public async Task<ApiResponse<List<ChannelInfo>>> GetUserChannels(Guid userId)
    {
        var accountIds = await _permissionRepository.GetAllowedChannelsAsync(userId);
        var result = new List<ChannelInfo>();

        foreach (var accountId in accountIds)
        {
            var account = await _accountRepository.GetByAccountIdAsync(accountId);
            if (account != null)
            {
                result.Add(new ChannelInfo
                {
                    AccountId = account.AccountId,
                    AccountName = account.Name,
                    MaxTps = 100,
                    Enabled = account.Enabled
                });
            }
        }

        return ApiResponse<List<ChannelInfo>>.Success(result);
    }

    [HttpPost("users/{userId}/prices")]
    public async Task<ApiResponse> SetUserPrices(Guid userId, [FromBody] SetUserPricesRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return ApiResponse.Fail(1, "User not found");
        }

        foreach (var price in request.Prices)
        {
            if (price.PricePerSegment <= 0)
            {
                return ApiResponse.Fail(1, $"Invalid price for {price.CountryCode}");
            }
            await _permissionRepository.SetUserCountryPriceAsync(userId, price.CountryCode, price.PricePerSegment);
        }

        _logger.LogInformation("Admin set custom prices for user {UserId}", userId);
        return ApiResponse.Success("Prices updated");
    }

    [HttpGet("users/{userId}/prices")]
    public async Task<ApiResponse<List<UserPriceInfo>>> GetUserPrices(Guid userId)
    {
        var prices = await _permissionRepository.GetUserPricesAsync(userId);
        var result = prices.Select(p => new UserPriceInfo
        {
            CountryCode = p.CountryCode,
            PricePerSegment = p.PricePerSegment
        }).ToList();

        return ApiResponse<List<UserPriceInfo>>.Success(result);
    }

    [HttpPost("users/{userId}/recharge")]
    public async Task<ApiResponse> Recharge(Guid userId, [FromBody] RechargeRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return ApiResponse.Fail(1, "User not found");
        }

        if (request.Amount <= 0)
        {
            return ApiResponse.Fail(1, "Amount must be positive");
        }

        await _billingService.RechargeAsync(userId, request.Amount, request.Description ?? "Admin recharge");

        _logger.LogInformation("Admin recharged {Amount} to user {UserId}", request.Amount, userId);
        return ApiResponse.Success("Recharged successfully");
    }

    [HttpGet("users/{userId}/balance")]
    public async Task<ApiResponse<object>> GetBalance(Guid userId)
    {
        var balance = await _billingService.GetBalanceAsync(userId);
        return ApiResponse<object>.Success(new { Balance = balance });
    }

    [HttpGet("countries")]
    public IActionResult GetCountries()
    {
        var countries = CountryCodeMapper.GetAllCountryCodes()
            .Select(code => CountryCodeMapper.GetCountryInfo(code))
            .Where(c => c != null)
            .Select(c => new
            {
                c!.CountryCode,
                c.Name,
                c.Prefix
            })
            .OrderBy(c => c.CountryCode);

        return Ok(ApiResponse<object>.Success(countries));
    }

    [HttpGet("channels")]
    public async Task<IActionResult> GetChannels()
    {
        var accounts = await _accountRepository.GetAllAsync();
        var result = accounts.Select(a => new
        {
            a.AccountId,
            a.Name,
            a.Host,
            a.Port,
            a.Weight,
            a.MaxTps,
            a.Enabled
        });

        return Ok(ApiResponse<object>.Success(result));
    }

    private async Task<UserDetailResponse> MapToUserDetailAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return new UserDetailResponse();
        }

        var countries = (await _permissionRepository.GetAllowedCountriesAsync(userId)).ToList();
        var channelIds = (await _permissionRepository.GetAllowedChannelsAsync(userId)).ToList();
        var prices = (await _permissionRepository.GetUserPricesAsync(userId)).ToList();

        var channels = new List<ChannelInfo>();
        foreach (var accountId in channelIds)
        {
            var account = await _accountRepository.GetByAccountIdAsync(accountId);
            if (account != null)
            {
                channels.Add(new ChannelInfo
                {
                    AccountId = account.AccountId,
                    AccountName = account.Name,
                    MaxTps = 100,
                    Enabled = account.Enabled
                });
            }
        }

        return new UserDetailResponse
        {
            Id = user.Id,
            Username = user.Username,
            Balance = user.Balance,
            Status = user.Status.ToString(),
            CreatedAt = user.CreatedAt,
            AllowedCountries = countries,
            AllowedChannels = channels,
            CustomPrices = prices.Select(p => new UserPriceInfo
            {
                CountryCode = p.CountryCode,
                PricePerSegment = p.PricePerSegment
            }).ToList()
        };
    }

    private UserDetailResponse MapToUserDetail(UserEntity user)
    {
        return new UserDetailResponse
        {
            Id = user.Id,
            Username = user.Username,
            Balance = user.Balance,
            Status = user.Status.ToString(),
            CreatedAt = user.CreatedAt,
            AllowedCountries = new List<string>(),
            AllowedChannels = new List<ChannelInfo>(),
            CustomPrices = new List<UserPriceInfo>()
        };
    }

    private static string GenerateApiKey(string username)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes($"{username}-{DateTime.UtcNow}-{Guid.NewGuid()}"));
        return Convert.ToHexString(bytes).ToLower()[..32];
    }

    private static string HashPassword(string password)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLower();
    }
}
