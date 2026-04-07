using Microsoft.EntityFrameworkCore;
using SmppStorage.Data;
using SmppStorage.Entities;

namespace SmppStorage.Repositories;

public interface IUserRepository
{
    Task<UserEntity?> GetByIdAsync(Guid id);
    Task<UserEntity?> GetByUsernameAsync(string username);
    Task<UserEntity?> GetByApiKeyAsync(string apiKey);
    Task<UserEntity> CreateAsync(UserEntity user);
    Task UpdateAsync(UserEntity user);
    Task UpdateBalanceAsync(Guid userId, decimal amount);
    Task<IEnumerable<UserEntity>> GetAllAsync();
}

public class UserRepository : IUserRepository
{
    private readonly SmppDbContext _context;

    public UserRepository(SmppDbContext context)
    {
        _context = context;
    }

    public async Task<UserEntity?> GetByIdAsync(Guid id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<UserEntity?> GetByUsernameAsync(string username)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<UserEntity?> GetByApiKeyAsync(string apiKey)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.ApiKey == apiKey);
    }

    public async Task<UserEntity> CreateAsync(UserEntity user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task UpdateAsync(UserEntity user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateBalanceAsync(Guid userId, decimal amount)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.Balance += amount;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<UserEntity>> GetAllAsync()
    {
        return await _context.Users.ToListAsync();
    }
}
