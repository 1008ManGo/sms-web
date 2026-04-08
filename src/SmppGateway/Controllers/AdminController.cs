using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmppClient.Utils;
using SmppGateway.Models;
using SmppGateway.Models.Admin;
using SmppGateway.Observability;
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
    private readonly ISmppClientManager _smppClientManager;
    private readonly IAlertService _alertService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IUserRepository userRepository,
        IPermissionRepository permissionRepository,
        IAccountRepository accountRepository,
        IBillingService billingService,
        IAuditLogRepository auditLogRepository,
        ISmppClientManager smppClientManager,
        IAlertService alertService,
        ILogger<AdminController> logger)
    {
        _userRepository = userRepository;
        _permissionRepository = permissionRepository;
        _accountRepository = accountRepository;
        _billingService = billingService;
        _auditLogRepository = auditLogRepository;
        _smppClientManager = smppClientManager;
        _alertService = alertService;
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

    [HttpPost("channels")]
    public async Task<IActionResult> CreateChannel([FromBody] CreateChannelRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.AccountId) || string.IsNullOrWhiteSpace(request.Host))
        {
            return BadRequest(ApiResponse.Fail(1, "AccountId and Host are required"));
        }

        if (await _accountRepository.ExistsAsync(request.AccountId))
        {
            return BadRequest(ApiResponse.Fail(1, "Channel already exists"));
        }

        var account = new SmppAccountEntity
        {
            Id = Guid.NewGuid(),
            AccountId = request.AccountId,
            Name = request.Name,
            Host = request.Host,
            Port = request.Port,
            SystemId = request.SystemId,
            Password = request.Password,
            SystemType = request.SystemType,
            Weight = request.Weight,
            Priority = request.Priority,
            MaxTps = request.MaxTps,
            MaxSessions = request.MaxSessions,
            Enabled = request.Enabled,
            CreatedAt = DateTime.UtcNow
        };

        await _accountRepository.CreateAsync(account);

        _logger.LogInformation("Admin created channel: {AccountId}", request.AccountId);
        return Ok(ApiResponse<ChannelResponse>.Success(new ChannelResponse
        {
            Id = account.Id,
            AccountId = account.AccountId,
            Name = account.Name,
            Host = account.Host,
            Port = account.Port,
            Weight = account.Weight,
            Priority = account.Priority,
            MaxTps = account.MaxTps,
            MaxSessions = account.MaxSessions,
            Enabled = account.Enabled,
            CreatedAt = account.CreatedAt
        }));
    }

    [HttpPut("channels/{accountId}")]
    public async Task<IActionResult> UpdateChannel(string accountId, [FromBody] UpdateChannelRequest request)
    {
        var account = await _accountRepository.GetByAccountIdAsync(accountId);
        if (account == null)
        {
            return NotFound(ApiResponse.Fail(1, "Channel not found"));
        }

        if (request.Name != null) account.Name = request.Name;
        if (request.Host != null) account.Host = request.Host;
        if (request.Port.HasValue) account.Port = request.Port.Value;
        if (request.SystemId != null) account.SystemId = request.SystemId;
        if (request.Password != null) account.Password = request.Password;
        if (request.Weight.HasValue) account.Weight = request.Weight.Value;
        if (request.Priority.HasValue) account.Priority = request.Priority;
        if (request.MaxTps.HasValue) account.MaxTps = request.MaxTps.Value;
        if (request.MaxSessions.HasValue) account.MaxSessions = request.MaxSessions.Value;

        await _accountRepository.UpdateAsync(account);

        _logger.LogInformation("Admin updated channel: {AccountId}", accountId);
        return Ok(ApiResponse.Success("Channel updated"));
    }

    [HttpDelete("channels/{accountId}")]
    public async Task<IActionResult> DeleteChannel(string accountId, [FromQuery] bool hard = false)
    {
        var account = await _accountRepository.GetByAccountIdAsync(accountId);
        if (account == null)
        {
            return NotFound(ApiResponse.Fail(1, "Channel not found"));
        }

        if (hard)
        {
            await _accountRepository.DeleteAsync(accountId);
            _logger.LogInformation("Admin hard deleted channel: {AccountId}", accountId);
            return Ok(ApiResponse.Success("Channel permanently deleted"));
        }
        else
        {
            account.Enabled = false;
            await _accountRepository.UpdateAsync(account);
            _logger.LogInformation("Admin soft deleted (disabled) channel: {AccountId}", accountId);
            return Ok(ApiResponse.Success("Channel disabled"));
        }
    }

    [HttpPost("channels/{accountId}/enable")]
    public async Task<IActionResult> EnableChannel(string accountId)
    {
        var account = await _accountRepository.GetByAccountIdAsync(accountId);
        if (account == null)
        {
            return NotFound(ApiResponse.Fail(1, "Channel not found"));
        }

        account.Enabled = true;
        await _accountRepository.UpdateAsync(account);

        _logger.LogInformation("Admin enabled channel: {AccountId}", accountId);
        return Ok(ApiResponse.Success("Channel enabled"));
    }

    [HttpPost("channels/{accountId}/disable")]
    public async Task<IActionResult> DisableChannel(string accountId)
    {
        var account = await _accountRepository.GetByAccountIdAsync(accountId);
        if (account == null)
        {
            return NotFound(ApiResponse.Fail(1, "Channel not found"));
        }

        account.Enabled = false;
        await _accountRepository.UpdateAsync(account);

        _logger.LogInformation("Admin disabled channel: {AccountId}", accountId);
        return Ok(ApiResponse.Success("Channel disabled"));
    }

    [HttpPut("channels/{accountId}/tps")]
    public async Task<IActionResult> UpdateChannelTps(string accountId, [FromBody] UpdateTpsRequest request)
    {
        var account = await _accountRepository.GetByAccountIdAsync(accountId);
        if (account == null)
        {
            return NotFound(ApiResponse.Fail(1, "Channel not found"));
        }

        if (request.MaxSessions.HasValue && request.MaxSessions.Value != account.MaxSessions)
        {
            var success = await _smppClientManager.UpdateAccountSessionsAsync(accountId, request.MaxSessions.Value);
            if (!success)
            {
                return BadRequest(ApiResponse.Fail(1, "Failed to update session count"));
            }
            account.MaxSessions = request.MaxSessions.Value;
        }

        if (request.MaxTps.HasValue && request.MaxTps.Value != account.MaxTps)
        {
            await _smppClientManager.UpdateAccountTpsAsync(accountId, request.MaxTps.Value);
            account.MaxTps = request.MaxTps.Value;
        }

        await _accountRepository.UpdateAsync(account);

        _logger.LogInformation("Admin updated channel TPS: {AccountId}, MaxTps: {MaxTps}, MaxSessions: {MaxSessions}", 
            accountId, request.MaxTps, request.MaxSessions);
        return Ok(ApiResponse.Success("Channel TPS updated"));
    }

    [HttpPost("channels/{accountId}/sessions/add")]
    public async Task<IActionResult> AddSession(string accountId)
    {
        var account = await _accountRepository.GetByAccountIdAsync(accountId);
        if (account == null)
        {
            return NotFound(ApiResponse.Fail(1, "Channel not found"));
        }

        var currentSessions = 0;
        var sessions = _smppClientManager.GetAccountSessions();
        if (sessions.TryGetValue(accountId, out var sessionList))
        {
            currentSessions = sessionList.Count(s => s == "connected");
        }

        if (currentSessions >= account.MaxSessions)
        {
            return BadRequest(ApiResponse.Fail(1, $"Already at max sessions ({account.MaxSessions})"));
        }

        var success = await _smppClientManager.AddSessionAsync(accountId);
        if (!success)
        {
            return BadRequest(ApiResponse.Fail(1, "Failed to add session"));
        }

        _logger.LogInformation("Admin added session to channel: {AccountId}", accountId);
        return Ok(ApiResponse.Success($"Session added, current: {currentSessions + 1}"));
    }

    [HttpPost("channels/{accountId}/sessions/remove")]
    public async Task<IActionResult> RemoveSession(string accountId)
    {
        var account = await _accountRepository.GetByAccountIdAsync(accountId);
        if (account == null)
        {
            return NotFound(ApiResponse.Fail(1, "Channel not found"));
        }

        var success = await _smppClientManager.RemoveSessionAsync(accountId);
        if (!success)
        {
            return BadRequest(ApiResponse.Fail(1, "No session to remove or failed to remove"));
        }

        _logger.LogInformation("Admin removed session from channel: {AccountId}", accountId);
        return Ok(ApiResponse.Success("Session removed"));
    }

    [HttpGet("channels/{accountId}/sessions")]
    public IActionResult GetChannelSessions(string accountId)
    {
        var sessions = _smppClientManager.GetAccountSessions();
        if (!sessions.TryGetValue(accountId, out var sessionList))
        {
            return Ok(ApiResponse<object>.Success(new { AccountId = accountId, Sessions = new List<string>() }));
        }

        return Ok(ApiResponse<object>.Success(new { AccountId = accountId, Sessions = sessionList }));
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

    [HttpGet("channels/{accountId}/stats")]
    public async Task<IActionResult> GetChannelStats(string accountId)
    {
        var account = await _accountRepository.GetByAccountIdAsync(accountId);
        if (account == null)
        {
            return NotFound(ApiResponse.Fail(1, "Channel not found"));
        }

        var routeStrategy = _smppClientManager.GetRouteStrategy() as dynamic;
        var pool = routeStrategy.GetPool(accountId) as dynamic;

        var stats = new ChannelStatsResponse
        {
            AccountId = account.AccountId,
            Name = account.Name,
            Enabled = account.Enabled,
            MaxTps = account.MaxTps
        };

        if (pool != null)
        {
            var sessions = pool.GetAllSessions() as IEnumerable<dynamic>;
            var sessionList = sessions?.ToList() ?? new List<dynamic>();

            stats.IsConnected = sessionList.Any(s => (bool)s.IsConnected);
            stats.ActiveSessions = sessionList.Count(s => (bool)s.IsConnected && (bool)s.IsBound);
            stats.TotalSessions = sessionList.Count;

            var healthySessions = sessionList.Where(s => (bool)s.IsConnected && (bool)s.IsBound).ToList();
            if (healthySessions.Any())
            {
                stats.PendingRequests = healthySessions.Sum(s => (int)((dynamic)s).WindowManager.PendingCount);
                stats.WindowUsagePercent = healthySessions
                    .Select(s => (double)((dynamic)s).WindowManager.UsagePercentage)
                    .DefaultIfEmpty(0)
                    .Average();
                stats.CurrentTps = (int)(stats.WindowUsagePercent / 100.0 * account.MaxTps);

                stats.Sessions = healthySessions.Select(s => new SessionStats
                {
                    SessionId = (string)((object)s).GetType().GetProperty("SessionId")?.GetValue(s) ?? "",
                    IsConnected = (bool)s.IsConnected,
                    IsBound = (bool)s.IsBound,
                    PendingCount = (int)((dynamic)s).WindowManager.PendingCount,
                    WindowSize = (int)((dynamic)s).WindowManager.WindowSize,
                    WindowUsagePercent = (double)((dynamic)s).WindowManager.UsagePercentage
                }).ToArray();
            }
        }

        var queueAdapter = GetQueueAdapter();
        if (queueAdapter != null)
        {
            stats.QueueStats = new QueueStats
            {
                SubmitQueueLength = queueAdapter.SubmitQueueLength,
                DlrQueueLength = queueAdapter.DlrQueueLength
            };
        }

        return Ok(ApiResponse<ChannelStatsResponse>.Success(stats));
    }

    [HttpGet("channels/stats")]
    public async Task<IActionResult> GetAllChannelsStats()
    {
        var accounts = await _accountRepository.GetAllAsync();
        var routeStrategy = _smppClientManager.GetRouteStrategy() as dynamic;
        var queueAdapter = GetQueueAdapter();

        var result = new List<ChannelStatsResponse>();
        var alerts = new List<ChannelAlertResponse>();

        foreach (var account in accounts)
        {
            var pool = routeStrategy?.GetPool(account.AccountId) as dynamic;
            var stats = new ChannelStatsResponse
            {
                AccountId = account.AccountId,
                Name = account.Name,
                Enabled = account.Enabled,
                MaxTps = account.MaxTps
            };

            if (pool != null)
            {
                var sessions = pool.GetAllSessions() as IEnumerable<dynamic>;
                var sessionList = sessions?.ToList() ?? new List<dynamic>();

                stats.IsConnected = sessionList.Any(s => (bool)s.IsConnected);
                stats.ActiveSessions = sessionList.Count(s => (bool)s.IsConnected && (bool)s.IsBound);
                stats.TotalSessions = sessionList.Count;

                var healthySessions = sessionList.Where(s => (bool)s.IsConnected && (bool)s.IsBound).ToList();
                if (healthySessions.Any())
                {
                    stats.PendingRequests = healthySessions.Sum(s => (int)((dynamic)s).WindowManager.PendingCount);
                    stats.WindowUsagePercent = healthySessions
                        .Select(s => (double)((dynamic)s).WindowManager.UsagePercentage)
                        .DefaultIfEmpty(0)
                        .Average();
                    stats.CurrentTps = (int)(stats.WindowUsagePercent / 100.0 * account.MaxTps);

                    stats.Sessions = healthySessions.Select(s => new SessionStats
                    {
                        SessionId = (string)((object)s).GetType().GetProperty("SessionId")?.GetValue(s) ?? "",
                        IsConnected = (bool)s.IsConnected,
                        IsBound = (bool)s.IsBound,
                        PendingCount = (int)((dynamic)s).WindowManager.PendingCount,
                        WindowSize = (int)((dynamic)s).WindowManager.WindowSize,
                        WindowUsagePercent = (double)((dynamic)s).WindowManager.UsagePercentage
                    }).ToArray();
                }
            }

            if (account.Enabled && !stats.IsConnected)
            {
                alerts.Add(new ChannelAlertResponse
                {
                    AccountId = account.AccountId,
                    AlertType = "ConnectionLost",
                    Severity = "Critical",
                    Message = $"Channel {account.Name} is enabled but not connected",
                    Timestamp = DateTime.UtcNow
                });
            }
            else if (stats.WindowUsagePercent > 80)
            {
                alerts.Add(new ChannelAlertResponse
                {
                    AccountId = account.AccountId,
                    AlertType = "HighLoad",
                    Severity = "Warning",
                    Message = $"Channel {account.Name} window usage is {stats.WindowUsagePercent:F1}%",
                    Timestamp = DateTime.UtcNow
                });
            }

            result.Add(stats);
        }

        var health = new SystemHealthResponse
        {
            TotalChannels = accounts.Count(),
            EnabledChannels = accounts.Count(a => a.Enabled),
            ConnectedChannels = result.Count(r => r.IsConnected),
            TotalSessions = result.Sum(r => r.TotalSessions),
            HealthySessions = result.Sum(r => r.ActiveSessions),
            TotalPendingRequests = result.Sum(r => r.PendingRequests),
            AverageWindowUsage = result.Where(r => r.ActiveSessions > 0).Select(r => r.WindowUsagePercent).DefaultIfEmpty(0).Average(),
            TotalQueueLength = queueAdapter?.SubmitQueueLength ?? 0,
            Alerts = alerts
        };

        return Ok(ApiResponse<object>.Success(new { Stats = result, Health = health }));
    }

    [HttpGet("health")]
    public async Task<IActionResult> GetSystemHealth()
    {
        var accounts = await _accountRepository.GetAllAsync();
        var routeStrategy = _smppClientManager.GetRouteStrategy() as dynamic;
        var queueAdapter = GetQueueAdapter();

        var alerts = new List<ChannelAlertResponse>();
        int connectedChannels = 0;
        int totalSessions = 0;
        int healthySessions = 0;
        int totalPending = 0;
        double totalWindowUsage = 0;
        int activeChannelsCount = 0;

        foreach (var account in accounts)
        {
            var pool = routeStrategy?.GetPool(account.AccountId) as dynamic;
            if (pool != null)
            {
                var sessions = pool.GetAllSessions() as IEnumerable<dynamic>;
                var sessionList = sessions?.ToList() ?? new List<dynamic>();
                var healthy = sessionList.Count(s => (bool)s.IsConnected && (bool)s.IsBound);

                if (healthy > 0)
                {
                    connectedChannels++;
                    totalSessions += sessionList.Count;
                    healthySessions += healthy;

                    var pending = sessionList.Where(s => (bool)s.IsConnected && (bool)s.IsBound)
                        .Sum(s => (int)((dynamic)s).WindowManager.PendingCount);
                    totalPending += pending;

                    var usage = sessionList.Where(s => (bool)s.IsConnected && (bool)s.IsBound)
                        .Select(s => (double)((dynamic)s).WindowManager.UsagePercentage)
                        .DefaultIfEmpty(0)
                        .Average();
                    totalWindowUsage += usage;
                    activeChannelsCount++;
                }
            }

            if (account.Enabled && (pool == null || healthySessions == 0))
            {
                alerts.Add(new ChannelAlertResponse
                {
                    AccountId = account.AccountId,
                    AlertType = "ConnectionLost",
                    Severity = "Critical",
                    Message = $"Channel {account.Name} is enabled but has no healthy sessions",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        var avgWindowUsage = activeChannelsCount > 0 ? totalWindowUsage / activeChannelsCount : 0;

        if (avgWindowUsage > 80)
        {
            alerts.Add(new ChannelAlertResponse
            {
                AccountId = "system",
                AlertType = "HighGlobalLoad",
                Severity = "Warning",
                Message = $"System average window usage is {avgWindowUsage:F1}%",
                Timestamp = DateTime.UtcNow
            });
        }

        if (queueAdapter != null && queueAdapter.SubmitQueueLength > 1000)
        {
            alerts.Add(new ChannelAlertResponse
            {
                AccountId = "system",
                AlertType = "QueueBacklog",
                Severity = "Warning",
                Message = $"Submit queue has {queueAdapter.SubmitQueueLength} messages pending",
                Timestamp = DateTime.UtcNow
            });
        }

        var health = new SystemHealthResponse
        {
            TotalChannels = accounts.Count(),
            EnabledChannels = accounts.Count(a => a.Enabled),
            ConnectedChannels = connectedChannels,
            TotalSessions = totalSessions,
            HealthySessions = healthySessions,
            TotalPendingRequests = totalPending,
            AverageWindowUsage = avgWindowUsage,
            TotalQueueLength = queueAdapter?.SubmitQueueLength ?? 0,
            Alerts = alerts
        };

        return Ok(ApiResponse<SystemHealthResponse>.Success(health));
    }

    private IQueueAdapter? GetQueueAdapter()
    {
        var field = _smppClientManager.GetType().GetField("_queueAdapter",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field?.GetValue(_smppClientManager) as IQueueAdapter;
    }

    [HttpGet("alerts")]
    public async Task<IActionResult> GetAlerts([FromQuery] string? accountId = null, [FromQuery] bool unresolvedOnly = false, [FromQuery] int limit = 100)
    {
        if (unresolvedOnly)
        {
            var unresolved = await _alertService.GetUnresolvedAlertsAsync();
            return Ok(ApiResponse<List<AlertEntity>>.Success(unresolved));
        }

        if (!string.IsNullOrEmpty(accountId))
        {
            var alerts = await _alertService.GetAlertsByAccountAsync(accountId, limit);
            return Ok(ApiResponse<List<AlertEntity>>.Success(alerts));
        }

        var allAlerts = await _alertService.GetAllAlertsAsync(limit);
        return Ok(ApiResponse<List<AlertEntity>>.Success(allAlerts));
    }

    [HttpPost("alerts/{alertId}/resolve")]
    public async Task<IActionResult> ResolveAlert(Guid alertId)
    {
        await _alertService.ResolveAlertAsync(alertId);
        _logger.LogInformation("Admin resolved alert {AlertId}", alertId);
        return Ok(ApiResponse.Success("Alert resolved"));
    }

    [HttpPost("channels/{accountId}/alerts/resolve")]
    public async Task<IActionResult> ResolveChannelAlerts(string accountId, [FromBody] ResolveChannelAlertsRequest request)
    {
        await _alertService.ResolveAlertsByAccountAsync(accountId, request.AlertType);
        _logger.LogInformation("Admin resolved {AlertType} alerts for channel {AccountId}", request.AlertType, accountId);
        return Ok(ApiResponse.Success($"Alerts of type {request.AlertType} resolved for channel {accountId}"));
    }
}

public class ResolveChannelAlertsRequest
{
    public AlertType AlertType { get; set; }
}
