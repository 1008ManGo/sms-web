namespace SmppGateway.Models.Admin;

public class CreateChannelRequest
{
    public string AccountId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 2775;
    public string SystemId { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string SystemType { get; set; } = "SMPP";
    public int Weight { get; set; } = 100;
    public int Priority { get; set; } = 1;
    public int MaxTps { get; set; } = 50;
    public int MaxSessions { get; set; } = 1;
    public bool Enabled { get; set; } = true;
}

public class UpdateChannelRequest
{
    public string? Name { get; set; }
    public string? Host { get; set; }
    public int? Port { get; set; }
    public string? SystemId { get; set; }
    public string? Password { get; set; }
    public int? Weight { get; set; }
    public int? Priority { get; set; }
    public int? MaxTps { get; set; }
    public int? MaxSessions { get; set; }
}

public class UpdateTpsRequest
{
    public int? MaxTps { get; set; }
    public int? MaxSessions { get; set; }
}

public class ChannelResponse
{
    public Guid Id { get; set; }
    public string AccountId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string SystemId { get; set; } = string.Empty;
    public int Weight { get; set; }
    public int Priority { get; set; }
    public int MaxTps { get; set; }
    public int MaxSessions { get; set; }
    public bool Enabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ActiveSessions { get; set; }
}

public class ChannelStatsResponse
{
    public string AccountId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public bool IsConnected { get; set; }
    public int ActiveSessions { get; set; }
    public int TotalSessions { get; set; }
    public int CurrentTps { get; set; }
    public int MaxTps { get; set; }
    public double WindowUsagePercent { get; set; }
    public int PendingRequests { get; set; }
    public QueueStats QueueStats { get; set; } = new();
    public SessionStats[] Sessions { get; set; } = [];
}

public class SessionStats
{
    public string SessionId { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public bool IsBound { get; set; }
    public int PendingCount { get; set; }
    public int WindowSize { get; set; }
    public double WindowUsagePercent { get; set; }
    public DateTime? LastActivity { get; set; }
}

public class QueueStats
{
    public int SubmitQueueLength { get; set; }
    public int DlrQueueLength { get; set; }
}

public class ChannelAlertResponse
{
    public string AccountId { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class SystemHealthResponse
{
    public int TotalChannels { get; set; }
    public int EnabledChannels { get; set; }
    public int ConnectedChannels { get; set; }
    public int TotalSessions { get; set; }
    public int HealthySessions { get; set; }
    public int TotalPendingRequests { get; set; }
    public double AverageWindowUsage { get; set; }
    public int TotalQueueLength { get; set; }
    public List<ChannelAlertResponse> Alerts { get; set; } = [];
}
