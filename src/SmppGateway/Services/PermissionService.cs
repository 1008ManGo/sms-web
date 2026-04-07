using SmppClient.Utils;
using SmppStorage.Repositories;

namespace SmppGateway.Services;

public class PermissionCheckResult
{
    public bool Allowed { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    public static PermissionCheckResult Success() => new() { Allowed = true };
    public static PermissionCheckResult Fail(string code, string message) => new() { Allowed = false, ErrorCode = code, ErrorMessage = message };
}

public interface IPermissionService
{
    Task<PermissionCheckResult> CheckSendPermissionAsync(Guid userId, string mobile);
    string ExtractCountryCode(string mobile);
}

public class PermissionService : IPermissionService
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(
        IPermissionRepository permissionRepository,
        ILogger<PermissionService> logger)
    {
        _permissionRepository = permissionRepository;
        _logger = logger;
    }

    public async Task<PermissionCheckResult> CheckSendPermissionAsync(Guid userId, string mobile)
    {
        var countryCode = ExtractCountryCode(mobile);

        var hasCountryPermission = await _permissionRepository.HasCountryPermissionAsync(userId, countryCode);
        if (!hasCountryPermission)
        {
            _logger.LogWarning("User {UserId} has no permission to send SMS to country {CountryCode}", 
                userId, countryCode);
            return PermissionCheckResult.Fail(
                "COUNTRY_NOT_ALLOWED",
                $"您没有向{countryCode} ({GetCountryName(countryCode)})发送短信的权限");
        }

        var allowedChannels = (await _permissionRepository.GetAllowedChannelsAsync(userId)).ToList();
        if (!allowedChannels.Any())
        {
            _logger.LogWarning("User {UserId} has no channel permissions", userId);
            return PermissionCheckResult.Fail(
                "NO_CHANNEL_ASSIGNED",
                "您没有分配任何发送通道，请联系管理员");
        }

        var hasAnyChannelPermission = await _permissionRepository.HasAnyPermissionAsync(userId);
        if (!hasAnyChannelPermission)
        {
            _logger.LogWarning("User {UserId} has no active permissions", userId);
            return PermissionCheckResult.Fail(
                "PERMISSION_DENIED",
                "您的账号权限已被禁用，请联系管理员");
        }

        return PermissionCheckResult.Success();
    }

    public string ExtractCountryCode(string mobile)
    {
        return CountryCodeMapper.ExtractCountryCode(mobile);
    }

    private static string GetCountryName(string countryCode)
    {
        var info = CountryCodeMapper.GetCountryInfo(countryCode);
        return info?.Name ?? "未知";
    }
}
