namespace SmppGateway.Models;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public UserStatus Status { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public DateTime CreatedAt { get; set; }
}

public enum UserStatus
{
    Active,
    Suspended,
    Deleted
}

public enum UserRole
{
    User,
    Admin
}

public class UserRegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UserLoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UserLoginResponse
{
    public string ApiKey { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}
