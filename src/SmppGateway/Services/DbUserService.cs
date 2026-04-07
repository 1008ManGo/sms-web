using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SmppGateway.Auth;
using SmppStorage.Data;
using SmppStorage.Entities;

namespace SmppGateway.Services;

public class DbUserService : IUserService
{
    private readonly SmppDbContext _context;
    private readonly ILogger<DbUserService> _logger;

    public DbUserService(SmppDbContext context, ILogger<DbUserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<UserEntity?> ValidateApiKeyAsync(string apiKey)
    {
        return await _context.Users.FirstOrDefaultAsync(u => 
            u.ApiKey == apiKey && u.Status == UserStatus.Active);
    }

    public async Task<UserEntity?> GetUserByIdAsync(Guid userId)
    {
        return await _context.Users.FindAsync(userId);
    }

    public async Task<UserEntity> CreateUserAsync(string username, string password)
    {
        if (await _context.Users.AnyAsync(u => u.Username == username))
        {
            throw new InvalidOperationException($"User {username} already exists");
        }

        var user = new UserEntity
        {
            Id = Guid.NewGuid(),
            Username = username,
            PasswordHash = HashPassword(password),
            ApiKey = GenerateApiKey(username),
            Balance = 0m,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created user: {Username}", username);
        return user;
    }

    public async Task<UserEntity?> LoginAsync(string username, string password)
    {
        var passwordHash = HashPassword(password);
        return await _context.Users.FirstOrDefaultAsync(u => 
            u.Username == username && u.PasswordHash == passwordHash);
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

    private static string GenerateApiKey(string username)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{username}-{DateTime.UtcNow}-{Guid.NewGuid()}"));
        return Convert.ToHexString(bytes).ToLower()[..32];
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLower();
    }
}
