using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmppStorage.Entities;

public class UserEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string ApiKey { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Balance { get; set; }

    public UserStatus Status { get; set; } = UserStatus.Active;

    public UserRole Role { get; set; } = UserRole.User;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}

public enum UserStatus
{
    Active = 0,
    Suspended = 1,
    Deleted = 2
}

public enum UserRole
{
    User = 0,
    Admin = 1
}

public class SmppAccountEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string AccountId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 2775;

    [Required]
    [MaxLength(100)]
    public string SystemId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [MaxLength(50)]
    public string SystemType { get; set; } = "SMPP";

    public int Weight { get; set; } = 100;
    public int Priority { get; set; } = 1;
    public int MaxTps { get; set; } = 50;
    public int MaxSessions { get; set; } = 1;
    public bool Enabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class SmsSubmitEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public UserEntity? User { get; set; }

    [Required]
    [MaxLength(50)]
    public string LocalId { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string MessageId { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Mobile { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public int SegmentCount { get; set; } = 1;

    [MaxLength(50)]
    public string? AccountId { get; set; }

    public decimal Cost { get; set; }

    public SmsStatus Status { get; set; } = SmsStatus.Pending;

    public DateTime SubmitTime { get; set; } = DateTime.UtcNow;

    public DateTime? DlrTime { get; set; }

    [MaxLength(20)]
    public string? ErrorCode { get; set; }

    [ForeignKey(nameof(AccountId))]
    public SmppAccountEntity? Account { get; set; }
}

public enum SmsStatus
{
    Pending = 0,
    Submitted = 1,
    Delivered = 2,
    Failed = 3,
    Expired = 4,
    Unknown = 5
}

public class PriceConfigEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string CountryCode { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,6)")]
    public decimal PricePerSegment { get; set; }

    public bool Enabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class DlrRecordEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string MessageId { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string LocalId { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Mobile { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public DlrStatus Status { get; set; } = DlrStatus.Pending;

    public DateTime SubmitTime { get; set; }

    public DateTime? DlrTime { get; set; }

    [MaxLength(20)]
    public string? ErrorCode { get; set; }

    [MaxLength(20)]
    public string? NetworkErrorCode { get; set; }

    public Guid? UserId { get; set; }

    public Guid? SmsSubmitId { get; set; }
}

public enum DlrStatus
{
    Pending = 0,
    Delivered = 1,
    Failed = 2,
    Expired = 3,
    Rejected = 4,
    Unknown = 5
}

public class AuditLogEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string EntityType { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? EntityId { get; set; }

    public Guid? UserId { get; set; }

    [MaxLength(500)]
    public string? Details { get; set; }

    [MaxLength(50)]
    public string IpAddress { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class UserCountryPermissionEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public UserEntity? User { get; set; }

    [Required]
    [MaxLength(20)]
    public string CountryCode { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class UserChannelPermissionEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public UserEntity? User { get; set; }

    [Required]
    [MaxLength(100)]
    public string AccountId { get; set; } = string.Empty;

    [ForeignKey(nameof(AccountId))]
    public SmppAccountEntity? Account { get; set; }

    public int MaxTps { get; set; } = 100;

    public bool Enabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class UserCountryPriceEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public UserEntity? User { get; set; }

    [Required]
    [MaxLength(20)]
    public string CountryCode { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,6)")]
    public decimal PricePerSegment { get; set; }

    public bool Enabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
