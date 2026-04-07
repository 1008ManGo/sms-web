using Microsoft.EntityFrameworkCore;
using SmppStorage.Data;
using SmppStorage.Entities;

namespace SmppStorage.Repositories;

public interface IPermissionRepository
{
    Task<bool> HasCountryPermissionAsync(Guid userId, string countryCode);
    Task<bool> HasChannelPermissionAsync(Guid userId, string accountId);
    Task<IEnumerable<string>> GetAllowedCountriesAsync(Guid userId);
    Task<IEnumerable<string>> GetAllowedChannelsAsync(Guid userId);
    Task AddCountryPermissionAsync(Guid userId, string countryCode);
    Task AddChannelPermissionAsync(Guid userId, string accountId, int maxTps = 100);
    Task RemoveCountryPermissionAsync(Guid userId, string countryCode);
    Task RemoveChannelPermissionAsync(Guid userId, string accountId);
    Task<bool> HasAnyPermissionAsync(Guid userId);
    Task<decimal> GetUserCountryPriceAsync(Guid userId, string countryCode);
    Task SetUserCountryPriceAsync(Guid userId, string countryCode, decimal pricePerSegment);
    Task<IEnumerable<UserCountryPriceEntity>> GetUserPricesAsync(Guid userId);
}

public class PermissionRepository : IPermissionRepository
{
    private readonly SmppDbContext _context;

    public PermissionRepository(SmppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> HasCountryPermissionAsync(Guid userId, string countryCode)
    {
        return await _context.UserCountryPermissions
            .AnyAsync(p => p.UserId == userId && p.CountryCode == countryCode && p.Enabled);
    }

    public async Task<bool> HasChannelPermissionAsync(Guid userId, string accountId)
    {
        return await _context.UserChannelPermissions
            .AnyAsync(p => p.UserId == userId && p.AccountId == accountId && p.Enabled);
    }

    public async Task<IEnumerable<string>> GetAllowedCountriesAsync(Guid userId)
    {
        return await _context.UserCountryPermissions
            .Where(p => p.UserId == userId && p.Enabled)
            .Select(p => p.CountryCode)
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetAllowedChannelsAsync(Guid userId)
    {
        return await _context.UserChannelPermissions
            .Where(p => p.UserId == userId && p.Enabled)
            .Select(p => p.AccountId)
            .ToListAsync();
    }

    public async Task AddCountryPermissionAsync(Guid userId, string countryCode)
    {
        var existing = await _context.UserCountryPermissions
            .FirstOrDefaultAsync(p => p.UserId == userId && p.CountryCode == countryCode);

        if (existing != null)
        {
            existing.Enabled = true;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _context.UserCountryPermissions.Add(new UserCountryPermissionEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CountryCode = countryCode,
                Enabled = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task AddChannelPermissionAsync(Guid userId, string accountId, int maxTps = 100)
    {
        var existing = await _context.UserChannelPermissions
            .FirstOrDefaultAsync(p => p.UserId == userId && p.AccountId == accountId);

        if (existing != null)
        {
            existing.Enabled = true;
            existing.MaxTps = maxTps;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _context.UserChannelPermissions.Add(new UserChannelPermissionEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AccountId = accountId,
                MaxTps = maxTps,
                Enabled = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task RemoveCountryPermissionAsync(Guid userId, string countryCode)
    {
        var permission = await _context.UserCountryPermissions
            .FirstOrDefaultAsync(p => p.UserId == userId && p.CountryCode == countryCode);

        if (permission != null)
        {
            permission.Enabled = false;
            permission.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task RemoveChannelPermissionAsync(Guid userId, string accountId)
    {
        var permission = await _context.UserChannelPermissions
            .FirstOrDefaultAsync(p => p.UserId == userId && p.AccountId == accountId);

        if (permission != null)
        {
            permission.Enabled = false;
            permission.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> HasAnyPermissionAsync(Guid userId)
    {
        var hasCountry = await _context.UserCountryPermissions
            .AnyAsync(p => p.UserId == userId && p.Enabled);
        var hasChannel = await _context.UserChannelPermissions
            .AnyAsync(p => p.UserId == userId && p.Enabled);
        return hasCountry && hasChannel;
    }

    public async Task<decimal> GetUserCountryPriceAsync(Guid userId, string countryCode)
    {
        var userPrice = await _context.UserCountryPrices
            .FirstOrDefaultAsync(p => p.UserId == userId && p.CountryCode == countryCode && p.Enabled);

        if (userPrice != null)
            return userPrice.PricePerSegment;

        var defaultPrice = await _context.PriceConfigs
            .FirstOrDefaultAsync(p => p.CountryCode == countryCode && p.Enabled);

        return defaultPrice?.PricePerSegment ?? 0.10m;
    }

    public async Task SetUserCountryPriceAsync(Guid userId, string countryCode, decimal pricePerSegment)
    {
        var existing = await _context.UserCountryPrices
            .FirstOrDefaultAsync(p => p.UserId == userId && p.CountryCode == countryCode);

        if (existing != null)
        {
            existing.PricePerSegment = pricePerSegment;
            existing.Enabled = true;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _context.UserCountryPrices.Add(new UserCountryPriceEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CountryCode = countryCode,
                PricePerSegment = pricePerSegment,
                Enabled = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<UserCountryPriceEntity>> GetUserPricesAsync(Guid userId)
    {
        return await _context.UserCountryPrices
            .Where(p => p.UserId == userId && p.Enabled)
            .ToListAsync();
    }
}
