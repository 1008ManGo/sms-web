using SmppClient.Utils;
using SmppStorage.Entities;
using SmppStorage.Repositories;

namespace SmppGateway.Services;

public interface IBillingService
{
    Task<decimal> CalculateCostAsync(Guid userId, string mobile, int segmentCount);
    Task<bool> ChargeAsync(Guid userId, decimal amount, string description);
    Task<decimal> GetBalanceAsync(Guid userId);
    Task RechargeAsync(Guid userId, decimal amount, string description);
}

public class BillingService : IBillingService
{
    private readonly IUserRepository _userRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<BillingService> _logger;

    public BillingService(
        IUserRepository userRepository,
        IPermissionRepository permissionRepository,
        IAuditLogRepository auditLogRepository,
        ILogger<BillingService> logger)
    {
        _userRepository = userRepository;
        _permissionRepository = permissionRepository;
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    public async Task<decimal> CalculateCostAsync(Guid userId, string mobile, int segmentCount)
    {
        var countryCode = CountryCodeMapper.ExtractCountryCode(mobile);
        var price = await _permissionRepository.GetUserCountryPriceAsync(userId, countryCode);
        return segmentCount * price;
    }

    public async Task<bool> ChargeAsync(Guid userId, decimal amount, string description)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Charge failed: user {UserId} not found", userId);
            return false;
        }

        if (user.Balance < amount)
        {
            _logger.LogWarning("Charge failed: insufficient balance for user {UserId}. Balance: {Balance}, Amount: {Amount}",
                userId, user.Balance, amount);
            return false;
        }

        await _userRepository.UpdateBalanceAsync(userId, -amount);

        await _auditLogRepository.CreateAsync(new AuditLogEntity
        {
            Id = Guid.NewGuid(),
            Action = "CHARGE",
            EntityType = "User",
            EntityId = userId.ToString(),
            UserId = userId,
            Details = $"Charge {amount} for {description}",
            IpAddress = "system"
        });

        _logger.LogInformation("Charged {Amount} from user {UserId} for {Description}", amount, userId, description);
        return true;
    }

    public async Task<decimal> GetBalanceAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        return user?.Balance ?? 0;
    }

    public async Task RechargeAsync(Guid userId, decimal amount, string description)
    {
        await _userRepository.UpdateBalanceAsync(userId, amount);

        await _auditLogRepository.CreateAsync(new AuditLogEntity
        {
            Id = Guid.NewGuid(),
            Action = "RECHARGE",
            EntityType = "User",
            EntityId = userId.ToString(),
            UserId = userId,
            Details = $"Recharge {amount} for {description}",
            IpAddress = "system"
        });

        _logger.LogInformation("Recharged {Amount} to user {UserId}", amount, userId);
    }
}
