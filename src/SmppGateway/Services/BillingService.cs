using SmppStorage.Entities;
using SmppStorage.Repositories;

namespace SmppGateway.Services;

public interface IBillingService
{
    Task<decimal> CalculateCostAsync(string mobile, int segmentCount);
    Task<bool> ChargeAsync(Guid userId, decimal amount, string description);
    Task<decimal> GetBalanceAsync(Guid userId);
    Task RechargeAsync(Guid userId, decimal amount, string description);
}

public class BillingService : IBillingService
{
    private readonly IUserRepository _userRepository;
    private readonly IPriceRepository _priceRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<BillingService> _logger;

    public BillingService(
        IUserRepository userRepository,
        IPriceRepository priceRepository,
        IAuditLogRepository auditLogRepository,
        ILogger<BillingService> logger)
    {
        _userRepository = userRepository;
        _priceRepository = priceRepository;
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    public async Task<decimal> CalculateCostAsync(string mobile, int segmentCount)
    {
        var countryCode = ExtractCountryCode(mobile);
        var price = await _priceRepository.GetByCountryCodeAsync(countryCode);

        if (price == null)
        {
            _logger.LogWarning("No price config for country code: {CountryCode}, using default", countryCode);
            return segmentCount * 0.10m;
        }

        return segmentCount * price.PricePerSegment;
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

    private static string ExtractCountryCode(string mobile)
    {
        mobile = mobile.Trim().Replace(" ", "").Replace("-", "");

        if (mobile.StartsWith("+"))
            mobile = mobile[1..];

        if (mobile.Length >= 2)
        {
            var potentialCode = mobile[..2];
            if (potentialCode == "86" || potentialCode == "1" || potentialCode == "81" || potentialCode == "82" ||
                potentialCode == "61" || potentialCode == "44" || potentialCode == "49")
            {
                return potentialCode;
            }
        }

        if (mobile.Length >= 3)
        {
            return mobile[..3];
        }

        return "86";
    }
}
