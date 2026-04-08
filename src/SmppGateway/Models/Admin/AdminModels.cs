namespace SmppGateway.Models.Admin;

public class AssignCountriesRequest
{
    public List<string> CountryCodes { get; set; } = new();
}

public class AssignChannelsRequest
{
    public List<ChannelAssignment> Channels { get; set; } = new();
}

public class ChannelAssignment
{
    public string AccountId { get; set; } = string.Empty;
    public int MaxTps { get; set; } = 100;
}

public class SetUserPricesRequest
{
    public List<CountryPrice> Prices { get; set; } = new();
}

public class CountryPrice
{
    public string CountryCode { get; set; } = string.Empty;
    public decimal PricePerSegment { get; set; }
}

public class UserDetailResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<string> AllowedCountries { get; set; } = new();
    public List<ChannelInfo> AllowedChannels { get; set; } = new();
    public List<UserPriceInfo> CustomPrices { get; set; } = new();
}

public class ChannelInfo
{
    public string AccountId { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public int MaxTps { get; set; }
    public bool Enabled { get; set; }
}

public class UserPriceInfo
{
    public string CountryCode { get; set; } = string.Empty;
    public decimal PricePerSegment { get; set; }
}

public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public decimal InitialBalance { get; set; } = 0m;
}

public class RechargeRequest
{
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}

public class BatchEnableChannelsRequest
{
    public List<string> AccountIds { get; set; } = new();
}

public class BatchDisableChannelsRequest
{
    public List<string> AccountIds { get; set; } = new();
}

public class BatchUpdateTpsRequest
{
    public List<ChannelTpsUpdate> Updates { get; set; } = new();
}

public class ChannelTpsUpdate
{
    public string AccountId { get; set; } = string.Empty;
    public int MaxTps { get; set; }
    public int? MaxSessions { get; set; }
}

public class BatchEnableUsersRequest
{
    public List<Guid> UserIds { get; set; } = new();
}

public class BatchDisableUsersRequest
{
    public List<Guid> UserIds { get; set; } = new();
}

public class BatchAssignCountriesRequest
{
    public List<Guid> UserIds { get; set; } = new();
    public List<string> CountryCodes { get; set; } = new();
}

public class BatchAssignChannelsRequest
{
    public List<Guid> UserIds { get; set; } = new();
    public List<ChannelAssignment> Channels { get; set; } = new();
}

public class BatchOperationResult
{
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> Failures { get; set; } = new();
    public List<string> SuccessItems { get; set; } = new();
}

public class ConfigureWebhookRequest
{
    public string Url { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
    public bool Enabled { get; set; } = true;
}
