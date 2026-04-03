using System.Diagnostics;
using System.Globalization;
using Finance.Api.Options;
using Microsoft.Extensions.Options;

namespace Finance.Api.Middleware;

/// <summary>
/// Adds X-Response-Time-Ms and Server-Timing for observability; logs slow requests vs configured budgets.
/// </summary>
public sealed class ResponseTimeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOptionsMonitor<PerformanceOptions> _options;
    private readonly ILogger<ResponseTimeMiddleware> _logger;

    public ResponseTimeMiddleware(
        RequestDelegate next,
        IOptionsMonitor<PerformanceOptions> options,
        ILogger<ResponseTimeMiddleware> logger)
    {
        _next = next;
        _options = options;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var opts = _options.CurrentValue;
        var start = Stopwatch.GetTimestamp();

        if (opts.ExposeResponseTimeHeaders)
        {
            context.Response.OnStarting(_ =>
            {
                var ms = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                var value = ms.ToString("F2", CultureInfo.InvariantCulture);
                context.Response.Headers["X-Response-Time-Ms"] = value;
                context.Response.Headers["Server-Timing"] = $"app;dur={ms:F1}";
                return Task.CompletedTask;
            }, 0);
        }

        await _next(context);

        var elapsedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        if (opts.ExposeResponseTimeHeaders && !context.Response.HasStarted)
        {
            var value = elapsedMs.ToString("F2", CultureInfo.InvariantCulture);
            context.Response.Headers["X-Response-Time-Ms"] = value;
            context.Response.Headers["Server-Timing"] = $"app;dur={elapsedMs:F1}";
        }
        if (ShouldLogSlowRequest(context.Request.Path, opts, elapsedMs))
        {
            if (elapsedMs >= opts.SlowRequestErrorThresholdMs)
            {
                _logger.LogError(
                    "Slow request (error threshold): {Method} {Path} full pipeline {ElapsedMs:F2}ms (limit {LimitMs}ms; X-Response-Time-Ms header is time-to-first-byte)",
                    context.Request.Method,
                    context.Request.Path.Value,
                    elapsedMs,
                    opts.SlowRequestErrorThresholdMs);
            }
            else if (elapsedMs >= opts.SlowRequestWarningThresholdMs)
            {
                _logger.LogWarning(
                    "Slow request (warning threshold): {Method} {Path} full pipeline {ElapsedMs:F2}ms (limit {LimitMs}ms; header ≈ TTFB)",
                    context.Request.Method,
                    context.Request.Path.Value,
                    elapsedMs,
                    opts.SlowRequestWarningThresholdMs);
            }
        }
    }

    private static bool ShouldLogSlowRequest(PathString path, PerformanceOptions opts, double elapsedMs)
    {
        if (elapsedMs < opts.SlowRequestWarningThresholdMs)
            return false;

        var p = path.Value ?? string.Empty;
        foreach (var ex in opts.ExcludedPathsFromSlowLogging)
        {
            if (p.Equals(ex, StringComparison.OrdinalIgnoreCase) ||
                p.StartsWith(ex + "/", StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }
}
