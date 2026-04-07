using Prometheus;
using SmppClient.Queue;
using SmppGateway.Auth;
using SmppGateway.Configuration;
using SmppGateway.Services;

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

builder.Services.AddSingleton<IUserService, InMemoryUserService>();
builder.Services.AddSingleton<ISmppClientManager, SmppClientManager>();

builder.Services.AddAuthentication(ApiKeyAuthenticationOptions.DefaultScheme)
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationOptions.DefaultScheme, _ => { });

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
        Description = "API Key authentication"
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

var smppClientManager = app.Services.GetRequiredService<ISmppClientManager>();
await smppClientManager.StartAsync();

app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpMetrics();

app.MapControllers();
app.MapMetrics();

var url = $"http://{config.Host}:{config.Port}";
app.Run(url);
