using Microsoft.EntityFrameworkCore;
using SmppStorage.Data;
using SmppStorage.Entities;

namespace SmppStorage.Repositories;

public interface IPriceRepository
{
    Task<PriceConfigEntity?> GetByCountryCodeAsync(string countryCode);
    Task<IEnumerable<PriceConfigEntity>> GetAllAsync();
    Task<PriceConfigEntity> CreateAsync(PriceConfigEntity price);
    Task UpdateAsync(PriceConfigEntity price);
    Task DeleteAsync(Guid id);
}

public class PriceRepository : IPriceRepository
{
    private readonly SmppDbContext _context;

    public PriceRepository(SmppDbContext context)
    {
        _context = context;
    }

    public async Task<PriceConfigEntity?> GetByCountryCodeAsync(string countryCode)
    {
        return await _context.PriceConfigs
            .FirstOrDefaultAsync(p => p.CountryCode == countryCode && p.Enabled);
    }

    public async Task<IEnumerable<PriceConfigEntity>> GetAllAsync()
    {
        return await _context.PriceConfigs.ToListAsync();
    }

    public async Task<PriceConfigEntity> CreateAsync(PriceConfigEntity price)
    {
        _context.PriceConfigs.Add(price);
        await _context.SaveChangesAsync();
        return price;
    }

    public async Task UpdateAsync(PriceConfigEntity price)
    {
        price.UpdatedAt = DateTime.UtcNow;
        _context.PriceConfigs.Update(price);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var price = await _context.PriceConfigs.FindAsync(id);
        if (price != null)
        {
            _context.PriceConfigs.Remove(price);
            await _context.SaveChangesAsync();
        }
    }
}

public interface IAccountRepository
{
    Task<SmppAccountEntity?> GetByAccountIdAsync(string accountId);
    Task<IEnumerable<SmppAccountEntity>> GetAllAsync();
    Task<IEnumerable<SmppAccountEntity>> GetEnabledAsync();
    Task<SmppAccountEntity> CreateAsync(SmppAccountEntity account);
    Task UpdateAsync(SmppAccountEntity account);
    Task DeleteAsync(string accountId);
}

public class AccountRepository : IAccountRepository
{
    private readonly SmppDbContext _context;

    public AccountRepository(SmppDbContext context)
    {
        _context = context;
    }

    public async Task<SmppAccountEntity?> GetByAccountIdAsync(string accountId)
    {
        return await _context.Accounts.FirstOrDefaultAsync(a => a.AccountId == accountId);
    }

    public async Task<IEnumerable<SmppAccountEntity>> GetAllAsync()
    {
        return await _context.Accounts.ToListAsync();
    }

    public async Task<IEnumerable<SmppAccountEntity>> GetEnabledAsync()
    {
        return await _context.Accounts.Where(a => a.Enabled).ToListAsync();
    }

    public async Task<SmppAccountEntity> CreateAsync(SmppAccountEntity account)
    {
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();
        return account;
    }

    public async Task UpdateAsync(SmppAccountEntity account)
    {
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string accountId)
    {
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountId == accountId);
        if (account != null)
        {
            _context.Accounts.Remove(account);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<SmppAccountEntity?> GetByIdAsync(Guid id)
    {
        return await _context.Accounts.FindAsync(id);
    }

    public async Task<bool> ExistsAsync(string accountId)
    {
        return await _context.Accounts.AnyAsync(a => a.AccountId == accountId);
    }
}

public interface IAuditLogRepository
{
    Task CreateAsync(AuditLogEntity log);
    Task<IEnumerable<AuditLogEntity>> GetByUserIdAsync(Guid userId, int limit = 100);
    Task<IEnumerable<AuditLogEntity>> GetByEntityAsync(string entityType, string entityId, int limit = 100);
}

public class AuditLogRepository : IAuditLogRepository
{
    private readonly SmppDbContext _context;

    public AuditLogRepository(SmppDbContext context)
    {
        _context = context;
    }

    public async Task CreateAsync(AuditLogEntity log)
    {
        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<AuditLogEntity>> GetByUserIdAsync(Guid userId, int limit = 100)
    {
        return await _context.AuditLogs
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLogEntity>> GetByEntityAsync(string entityType, string entityId, int limit = 100)
    {
        return await _context.AuditLogs
            .Where(l => l.EntityType == entityType && l.EntityId == entityId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }
}

public interface IAlertRepository
{
    Task<AlertEntity> CreateAsync(AlertEntity alert);
    Task<AlertEntity?> GetByIdAsync(Guid id);
    Task<List<AlertEntity>> GetUnresolvedAsync();
    Task<List<AlertEntity>> GetByAccountIdAsync(string accountId, int limit = 100);
    Task<List<AlertEntity>> GetAllAsync(int limit = 100);
    Task ResolveAsync(Guid id);
    Task ResolveByAccountAsync(string accountId, AlertType? type = null);
}

public class AlertRepository : IAlertRepository
{
    private readonly SmppDbContext _context;

    public AlertRepository(SmppDbContext context)
    {
        _context = context;
    }

    public async Task<AlertEntity> CreateAsync(AlertEntity alert)
    {
        _context.Alerts.Add(alert);
        await _context.SaveChangesAsync();
        return alert;
    }

    public async Task<AlertEntity?> GetByIdAsync(Guid id)
    {
        return await _context.Alerts.FindAsync(id);
    }

    public async Task<List<AlertEntity>> GetUnresolvedAsync()
    {
        return await _context.Alerts
            .Where(a => !a.IsResolved)
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Severity)
            .ToListAsync();
    }

    public async Task<List<AlertEntity>> GetByAccountIdAsync(string accountId, int limit = 100)
    {
        return await _context.Alerts
            .Where(a => a.AccountId == accountId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<AlertEntity>> GetAllAsync(int limit = 100)
    {
        return await _context.Alerts
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task ResolveAsync(Guid id)
    {
        var alert = await _context.Alerts.FindAsync(id);
        if (alert != null)
        {
            alert.IsResolved = true;
            alert.ResolvedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task ResolveByAccountAsync(string accountId, AlertType? type = null)
    {
        var query = _context.Alerts.Where(a => a.AccountId == accountId && !a.IsResolved);
        
        if (type.HasValue)
        {
            query = query.Where(a => a.Type == type.Value);
        }

        var alerts = await query.ToListAsync();
        foreach (var alert in alerts)
        {
            alert.IsResolved = true;
            alert.ResolvedAt = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();
    }
}
