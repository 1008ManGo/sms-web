using Microsoft.EntityFrameworkCore;
using SmppStorage.Entities;

namespace SmppStorage.Data;

public class SmppDbContext : DbContext
{
    public SmppDbContext(DbContextOptions<SmppDbContext> options) : base(options)
    {
    }

    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<SmppAccountEntity> Accounts => Set<SmppAccountEntity>();
    public DbSet<SmsSubmitEntity> SmsSubmits => Set<SmsSubmitEntity>();
    public DbSet<PriceConfigEntity> PriceConfigs => Set<PriceConfigEntity>();
    public DbSet<DlrRecordEntity> DlrRecords => Set<DlrRecordEntity>();
    public DbSet<AuditLogEntity> AuditLogs => Set<AuditLogEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.ApiKey).IsUnique();
        });

        modelBuilder.Entity<SmsSubmitEntity>(entity =>
        {
            entity.HasIndex(e => e.LocalId);
            entity.HasIndex(e => e.MessageId);
            entity.HasIndex(e => e.Mobile);
            entity.HasIndex(e => e.SubmitTime);
            entity.HasIndex(e => new { e.UserId, e.SubmitTime });
        });

        modelBuilder.Entity<DlrRecordEntity>(entity =>
        {
            entity.HasIndex(e => e.MessageId);
            entity.HasIndex(e => e.LocalId);
            entity.HasIndex(e => e.Mobile);
        });

        modelBuilder.Entity<PriceConfigEntity>(entity =>
        {
            entity.HasIndex(e => e.CountryCode).IsUnique();
        });

        modelBuilder.Entity<SmppAccountEntity>(entity =>
        {
            entity.HasIndex(e => e.AccountId).IsUnique();
        });

        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        var defaultUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var defaultAccountId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        modelBuilder.Entity<UserEntity>().HasData(new UserEntity
        {
            Id = defaultUserId,
            Username = "testuser",
            PasswordHash = "5e884898da28047d6fe8b15ed9c4e50e6ff2a0f9d4e6c4e6ff2a0f9d4e6c4e6ff",
            ApiKey = "a1b2c3d4e5f678901234567890123456",
            Balance = 10000m,
            Status = UserStatus.Active,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        modelBuilder.Entity<SmppAccountEntity>().HasData(new SmppAccountEntity
        {
            Id = defaultAccountId,
            AccountId = "account-1",
            Name = "运营商1",
            Host = "127.0.0.1",
            Port = 2775,
            SystemId = "smppclient",
            Password = "password",
            SystemType = "SMPP",
            Weight = 100,
            Priority = 1,
            MaxTps = 100,
            MaxSessions = 2,
            Enabled = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        modelBuilder.Entity<PriceConfigEntity>().HasData(
            new PriceConfigEntity
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                CountryCode = "86",
                PricePerSegment = 0.05m,
                Enabled = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new PriceConfigEntity
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                CountryCode = "1",
                PricePerSegment = 0.10m,
                Enabled = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
