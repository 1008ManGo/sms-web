using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prometheus;
using SmppClient.Queue;
using SmppGateway.Auth;
using SmppGateway.Configuration;
using SmppGateway.Observability;
using SmppGateway.Services;
using SmppStorage.Data;
using SmppStorage.Repositories;

var builder = WebApplication.CreateBuilder(args);

var configPath = Path.Combine(AppContext.BaseDirectory, "config.json");
AppConfig config;

if (File.Exists(configPath))
{
    var json = File.ReadAllText(configPath);
    config = System.Text.Json.JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
}
else
{
    config = new AppConfig();
}

builder.Services.AddSingleton(config);

builder.Services.AddDbContext<SmppDbContext>(options =>
    options.UseNpgsql(config.GetConnectionString()));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISmsSubmitRepository, SmsSubmitRepository>();
builder.Services.AddScoped<IDlrRepository, DlrRepository>();
builder.Services.AddScoped<IPriceRepository, PriceRepository>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();
builder.Services.AddScoped<IUserService, DbUserService>();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<ISubmitRecordService, SubmitRecordService>();
builder.Services.AddScoped<IDlrRecordService, DlrRecordService>();

builder.Services.AddScoped<IAlertService, AlertService>();

builder.Services.AddSingleton<ISmppClientManager, SmppClientManager>();

builder.Services.AddSingleton<DlrEventHandler>();

builder.Services.AddSingleton<MetricsCollector>();

builder.Services.AddHealthChecks()
    .AddCheck<SmppHealthCheck>("smpp_sessions", tags: new[] { "smpp" })
    .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "db" })
    .AddCheck<QueueHealthCheck>("queue", tags: new[] { "queue" });

builder.Services.AddAuthentication()
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationOptions.DefaultScheme, _ => { })
    .AddScheme<AdminAuthenticationOptions, AdminAuthenticationHandler>(
        AdminAuthenticationOptions.DefaultScheme, _ => { });

builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "SMPP Gateway API", Version = "v1" });
    
    c.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "X-Api-Key",
        Description = "User API Key authentication"
    });
    
    c.AddSecurityDefinition("AdminKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "X-Admin-Key",
        Description = "Admin API Key authentication"
    });
    
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SmppDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

var smppClientManager = app.Services.GetRequiredService<ISmppClientManager>();
var alertService = app.Services.GetRequiredService<IAlertService>();
smppClientManager.SetAlertService(alertService);
await smppClientManager.StartAsync();

var metricsCollector = app.Services.GetRequiredService<MetricsCollector>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpMetrics();
app.UseMetrics();

app.MapHealthChecks("/healthz", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = new
        {
            Status = report.Status.ToString(),
            Timestamp = DateTime.UtcNow,
            Duration = report.TotalDuration.TotalMilliseconds,
            Checks = report.Entries.Select(e => new
            {
                Name = e.Key,
                Status = e.Value.Status.ToString(),
                Duration = e.Value.Duration.TotalMilliseconds,
                Description = e.Value.Description
            })
        };
        await context.Response.WriteAsJsonAsync(result);
    }
});

app.MapControllers();
app.MapMetrics();

var url = $"http://{config.Host}:{config.Port}";
app.Run(url);
