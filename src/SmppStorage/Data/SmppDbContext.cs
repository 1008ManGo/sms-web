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
    public DbSet<AlertEntity> Alerts => Set<AlertEntity>();
    public DbSet<UserCountryPermissionEntity> UserCountryPermissions => Set<UserCountryPermissionEntity>();
    public DbSet<UserChannelPermissionEntity> UserChannelPermissions => Set<UserChannelPermissionEntity>();
    public DbSet<UserCountryPriceEntity> UserCountryPrices => Set<UserCountryPriceEntity>();

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

        modelBuilder.Entity<UserCountryPermissionEntity>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.CountryCode }).IsUnique();
            entity.HasIndex(e => e.CountryCode);
        });

        modelBuilder.Entity<UserChannelPermissionEntity>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.AccountId }).IsUnique();
        });

        modelBuilder.Entity<UserCountryPriceEntity>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.CountryCode }).IsUnique();
        });

        modelBuilder.Entity<AlertEntity>(entity =>
        {
            entity.HasIndex(e => e.AccountId);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Severity);
            entity.HasIndex(e => e.IsResolved);
            entity.HasIndex(e => e.CreatedAt);
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
            Role = UserRole.Admin,
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

        // 新用户默认不分配国家和通道，需要管理员手动配置

        modelBuilder.Entity<PriceConfigEntity>().HasData(
            new PriceConfigEntity { Id = Guid.Parse("33333333-3333-3333-3333-333333333331"), CountryCode = "86", PricePerSegment = 0.05m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("33333333-3333-3333-3333-333333333332"), CountryCode = "1", PricePerSegment = 0.10m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), CountryCode = "44", PricePerSegment = 0.12m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("33333333-3333-3333-3333-333333333334"), CountryCode = "81", PricePerSegment = 0.15m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("33333333-3333-3333-3333-333333333335"), CountryCode = "82", PricePerSegment = 0.12m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("33333333-3333-3333-3333-333333333336"), CountryCode = "60", PricePerSegment = 0.08m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("33333333-3333-3333-3333-333333333337"), CountryCode = "65", PricePerSegment = 0.10m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("33333333-3333-3333-3333-333333333338"), CountryCode = "63", PricePerSegment = 0.08m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("33333333-3333-3333-3333-333333333339"), CountryCode = "62", PricePerSegment = 0.06m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("33333333-3333-3333-3333-33333333333a"), CountryCode = "84", PricePerSegment = 0.06m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("33333333-3333-3333-3333-33333333333b"), CountryCode = "66", PricePerSegment = 0.07m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("33333333-3333-3333-3333-33333333333c"), CountryCode = "55", PricePerSegment = 0.10m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("33333333-3333-3333-3333-33333333333d"), CountryCode = "54", PricePerSegment = 0.10m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("33333333-3333-3333-3333-33333333333e"), CountryCode = "56", PricePerSegment = 0.10m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("33333333-3333-3333-3333-33333333333f"), CountryCode = "52", PricePerSegment = 0.10m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("33333333-3333-3333-3333-33333333333g"), CountryCode = "51", PricePerSegment = 0.10m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("33333333-3333-3333-3333-33333333333h"), CountryCode = "91", PricePerSegment = 0.05m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("33333333-3333-3333-3333-33333333333i"), CountryCode = "92", PricePerSegment = 0.05m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("33333333-3333-3333-3333-33333333333j"), CountryCode = "94", PricePerSegment = 0.05m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("33333333-3333-3333-3333-33333333333k"), CountryCode = "880", PricePerSegment = 0.04m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("33333333-3333-3333-3333-33333333333l"), CountryCode = "855", PricePerSegment = 0.04m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("33333333-3333-3333-3333-33333333333m"), CountryCode = "856", PricePerSegment = 0.04m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("33333333-3333-3333-3333-33333333333n"), CountryCode = "95", PricePerSegment = 0.04m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },

            // 欧洲
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444401"), CountryCode = "33", PricePerSegment = 0.12m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444402"), CountryCode = "34", PricePerSegment = 0.10m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444403"), CountryCode = "39", PricePerSegment = 0.12m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444404"), CountryCode = "49", PricePerSegment = 0.10m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444405"), CountryCode = "7", PricePerSegment = 0.15m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444406"), CountryCode = "971", PricePerSegment = 0.12m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444407"), CountryCode = "966", PricePerSegment = 0.10m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444408"), CountryCode = "20", PricePerSegment = 0.08m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444409"), CountryCode = "27", PricePerSegment = 0.10m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-44444444440a"), CountryCode = "234", PricePerSegment = 0.08m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-44444444440b"), CountryCode = "61", PricePerSegment = 0.10m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-44444444440c"), CountryCode = "64", PricePerSegment = 0.10m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-44444444440d"), CountryCode = "31", PricePerSegment = 0.10m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-44444444440e"), CountryCode = "32", PricePerSegment = 0.10m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-44444444440f"), CountryCode = "41", PricePerSegment = 0.12m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444410"), CountryCode = "43", PricePerSegment = 0.12m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444411"), CountryCode = "45", PricePerSegment = 0.12m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444412"), CountryCode = "46", PricePerSegment = 0.11m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444413"), CountryCode = "47", PricePerSegment = 0.12m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444414"), CountryCode = "48", PricePerSegment = 0.08m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444415"), CountryCode = "351", PricePerSegment = 0.10m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444416"), CountryCode = "358", PricePerSegment = 0.11m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444417"), CountryCode = "90", PricePerSegment = 0.07m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444418"), CountryCode = "92", PricePerSegment = 0.05m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444419"), CountryCode = "98", PricePerSegment = 0.08m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-44444444441a"), CountryCode = "961", PricePerSegment = 0.10m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-44444444441b"), CountryCode = "962", PricePerSegment = 0.10m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-44444444441c"), CountryCode = "963", PricePerSegment = 0.12m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-44444444441d"), CountryCode = "964", PricePerSegment = 0.10m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-44444444441e"), CountryCode = "965", PricePerSegment = 0.10m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-44444444441f"), CountryCode = "967", PricePerSegment = 0.10m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444420"), CountryCode = "968", PricePerSegment = 0.12m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444421"), CountryCode = "973", PricePerSegment = 0.10m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444422"), CountryCode = "974", PricePerSegment = 0.12m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444423"), CountryCode = "212", PricePerSegment = 0.08m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444424"), CountryCode = "213", PricePerSegment = 0.08m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444425"), CountryCode = "216", PricePerSegment = 0.08m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444426"), CountryCode = "218", PricePerSegment = 0.10m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444427"), CountryCode = "220", PricePerSegment = 0.06m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444428"), CountryCode = "221", PricePerSegment = 0.06m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444429"), CountryCode = "222", PricePerSegment = 0.06m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-44444444442a"), CountryCode = "223", PricePerSegment = 0.06m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-44444444442b"), CountryCode = "224", PricePerSegment = 0.06m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-44444444442c"), CountryCode = "225", PricePerSegment = 0.06m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-44444444442d"), CountryCode = "226", PricePerSegment = 0.05m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-44444444442e"), CountryCode = "227", PricePerSegment = 0.05m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-44444444442f"), CountryCode = "228", PricePerSegment = 0.05m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444430"), CountryCode = "229", PricePerSegment = 0.05m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444431"), CountryCode = "230", PricePerSegment = 0.10m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444432"), CountryCode = "231", PricePerSegment = 0.08m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444433"), CountryCode = "233", PricePerSegment = 0.08m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444434"), CountryCode = "234", PricePerSegment = 0.08m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444435"), CountryCode = "237", PricePerSegment = 0.06m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444436"), CountryCode = "251", PricePerSegment = 0.06m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444437"), CountryCode = "254", PricePerSegment = 0.06m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444438"), CountryCode = "255", PricePerSegment = 0.05m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444439"), CountryCode = "256", PricePerSegment = 0.05m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-44444444443a"), CountryCode = "258", PricePerSegment = 0.06m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-44444444443b"), CountryCode = "260", PricePerSegment = 0.04m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-44444444443c"), CountryCode = "261", PricePerSegment = 0.06m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-44444444443d"), CountryCode = "263", PricePerSegment = 0.05m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-44444444443e"), CountryCode = "264", PricePerSegment = 0.08m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-44444444443f"), CountryCode = "265", PricePerSegment = 0.05m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444440"), CountryCode = "266", PricePerSegment = 0.08m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444441"), CountryCode = "267", PricePerSegment = 0.05m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444442"), CountryCode = "268", PricePerSegment = 0.08m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PriceConfigEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444443"), CountryCode = "269", PricePerSegment = 0.06m, Enabled = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}
