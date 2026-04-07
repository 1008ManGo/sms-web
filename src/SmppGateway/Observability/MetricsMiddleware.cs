using System.Diagnostics;
using SmppGateway.Observability;

namespace SmppGateway.Observability;

public class MetricsMiddleware
{
    private readonly RequestDelegate _next;

    public MetricsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, MetricsCollector metricsCollector)
    {
        if (context.Request.Path.StartsWithSegments("/metrics") ||
            context.Request.Path.StartsWithSegments("/swagger"))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var endpoint = context.Request.Path.Value ?? "/";
            var method = context.Request.Method;
            var status = context.Response.StatusCode;
            var duration = stopwatch.Elapsed.TotalSeconds;

            metricsCollector.RecordApiRequest(method, endpoint, status, duration);
        }
    }
}

public static class MetricsMiddlewareExtensions
{
    public static IApplicationBuilder UseMetrics(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<MetricsMiddleware>();
    }
}
