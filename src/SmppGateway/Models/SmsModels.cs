using System.ComponentModel.DataAnnotations;

namespace SmppGateway.Models;

public class SubmitSmsRequest
{
    [Required]
    [RegularExpression(@"^\+?[0-9]{6,15}$", ErrorMessage = "Invalid mobile number")]
    public string Mobile { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    public string? Ext { get; set; }
}

public class BatchSubmitSmsRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(1000)]
    public List<BatchSmsItem> List { get; set; } = new();
}

public class BatchSmsItem
{
    [Required]
    [RegularExpression(@"^\+?[0-9]{6,15}$", ErrorMessage = "Invalid mobile number")]
    public string Mobile { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    public string? Ext { get; set; }
}

public class SubmitSmsResponse
{
    public string MessageId { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
}

public class BatchSubmitSmsResponse
{
    public int SuccessCount { get; set; }
    public int FailCount { get; set; }
    public List<SubmitSmsResponse> Results { get; set; } = new();
}
