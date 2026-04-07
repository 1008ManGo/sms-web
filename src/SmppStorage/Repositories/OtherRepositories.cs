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
        _context.Accounts.Update(account);
        await _context.SaveChangesAsync();
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
