using Microsoft.AspNetCore.Mvc;
using SmppGateway.Models;
using SmppGateway.Services;

namespace SmppGateway.Controllers;

[ApiController]
[Route("api/v1/user")]
public class UserController : ControllerBase
{
    private readonly InMemoryUserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(
        InMemoryUserService userService,
        ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ApiResponse<UserLoginResponse>> Register([FromBody] UserRegisterRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return ApiResponse<UserLoginResponse>.Fail(1, "Username and password are required");
            }

            var user = await _userService.CreateUserAsync(request.Username, request.Password);

            _logger.LogInformation("User registered: {Username}", request.Username);

            return ApiResponse<UserLoginResponse>.Success(new UserLoginResponse
            {
                ApiKey = user.ApiKey,
                Balance = user.Balance
            });
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponse<UserLoginResponse>.Fail(1, ex.Message);
        }
    }

    [HttpPost("login")]
    public async Task<ApiResponse<UserLoginResponse>> Login([FromBody] UserLoginRequest request)
    {
        var user = await _userService.LoginAsync(request.Username, request.Password);

        if (user == null)
        {
            return ApiResponse<UserLoginResponse>.Fail(1, "Invalid username or password");
        }

        return ApiResponse<UserLoginResponse>.Success(new UserLoginResponse
        {
            ApiKey = user.ApiKey,
            Balance = user.Balance
        });
    }
}
