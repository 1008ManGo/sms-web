namespace SmppGateway.Configuration;

public class AppConfig
{
    public string Host { get; set; } = "0.0.0.0";
    public int Port { get; set; } = 8080;
    public string AdminApiKey { get; set; } = "admin-a1b2c3d4e5f678901234567890123456";
    public DatabaseConfig Database { get; set; } = new();
    public RabbitMqConfig RabbitMq { get; set; } = new();
    public List<SmppAccountConfig> Accounts { get; set; } = new();

    public string GetConnectionString()
    {
        return $"Host={Database.Host};Port={Database.Port};Database={Database.Database};Username={Database.Username};Password={Database.Password}";
    }
}

public class DatabaseConfig
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5432;
    public string Username { get; set; } = "postgres";
    public string Password { get; set; } = "postgres";
    public string Database { get; set; } = "smpp_gateway";
}

public class RabbitMqConfig
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
}

public class SmppAccountConfig
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 2775;
    public string SystemId { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string SystemType { get; set; } = "SMPP";
    public int Weight { get; set; } = 100;
    public int Priority { get; set; } = 1;
    public int MaxTps { get; set; } = 100;
    public int MaxSessions { get; set; } = 1;
    public bool Enabled { get; set; } = true;
}
