using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using SmppGateway.Auth;
using SmppGateway.Models;

namespace SmppGateway.Services;

public class InMemoryUserService : IUserService
{
    private class InternalUser
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public UserStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private readonly ConcurrentDictionary<Guid, InternalUser> _users = new();
    private readonly ConcurrentDictionary<string, Guid> _apiKeys = new();
    private readonly ILogger<InMemoryUserService> _logger;

    public InMemoryUserService(ILogger<InMemoryUserService> logger)
    {
        _logger = logger;
        CreateDefaultUser();
    }

    private void CreateDefaultUser()
    {
        var user = new InternalUser
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            ApiKey = GenerateApiKey("testuser"),
            PasswordHash = HashPassword("password"),
            Balance = 10000m,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _users[user.Id] = user;
        _apiKeys[user.ApiKey] = user.Id;
        _logger.LogInformation("Created default user: {Username}", user.Username);
    }

    public Task<User?> ValidateApiKeyAsync(string apiKey)
    {
        if (_apiKeys.TryGetValue(apiKey, out var userId))
        {
            _users.TryGetValue(userId, out var user);
            return Task.FromResult(user != null ? ToModel(user) : null);
        }
        return Task.FromResult<User?>(null);
    }

    public Task<User?> GetUserByIdAsync(Guid userId)
    {
        _users.TryGetValue(userId, out var user);
        return Task.FromResult(user != null ? ToModel(user) : null);
    }

    public Task<User> CreateUserAsync(string username, string password)
    {
        if (_users.Values.Any(u => u.Username == username))
        {
            throw new InvalidOperationException($"User {username} already exists");
        }

        var user = new InternalUser
        {
            Id = Guid.NewGuid(),
            Username = username,
            ApiKey = GenerateApiKey(username),
            PasswordHash = HashPassword(password),
            Balance = 0m,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _users[user.Id] = user;
        _apiKeys[user.ApiKey] = user.Id;

        _logger.LogInformation("Created user: {Username}", username);
        return Task.FromResult(ToModel(user));
    }

    public Task<User?> LoginAsync(string username, string password)
    {
        var user = _users.Values.FirstOrDefault(u => 
            u.Username == username && u.PasswordHash == HashPassword(password));

        return Task.FromResult(user != null ? ToModel(user) : null);
    }

    public Task UpdateBalanceAsync(Guid userId, decimal amount)
    {
        if (_users.TryGetValue(userId, out var user))
        {
            user.Balance += amount;
        }
        return Task.CompletedTask;
    }

    public Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return Task.FromResult(_users.Values.Select(ToModel).AsEnumerable());
    }

    private static User ToModel(InternalUser entity)
    {
        return new User
        {
            Id = entity.Id,
            Username = entity.Username,
            ApiKey = entity.ApiKey,
            Balance = entity.Balance,
            Status = entity.Status,
            CreatedAt = entity.CreatedAt
        };
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
