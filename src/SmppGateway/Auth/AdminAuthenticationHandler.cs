using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using SmppStorage.Entities;
using SmppGateway.Services;

namespace SmppGateway.Auth;

public class AdminAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "AdminKey";
    public const string HeaderName = "X-Api-Key";
}

public class AdminAuthenticationHandler : AuthenticationHandler<AdminAuthenticationOptions>
{
    private readonly IUserService _userService;

    public AdminAuthenticationHandler(
        IOptionsMonitor<AdminAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IUserService userService)
        : base(options, logger, encoder)
    {
        _userService = userService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(AdminAuthenticationOptions.HeaderName, out var apiKeyHeaderValues))
        {
            return AuthenticateResult.Fail("API Key not found");
        }

        var providedApiKey = apiKeyHeaderValues.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(providedApiKey))
        {
            return AuthenticateResult.Fail("API Key not found");
        }

        var user = await _userService.ValidateApiKeyAsync(providedApiKey);

        if (user == null)
        {
            return AuthenticateResult.Fail("Invalid API Key");
        }

        if (user.Status != UserStatus.Active)
        {
            return AuthenticateResult.Fail("User is not active");
        }

        if (user.Role != UserRole.Admin)
        {
            return AuthenticateResult.Fail("Admin access required");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("ApiKey", user.ApiKey),
            new Claim("Balance", user.Balance.ToString())
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
