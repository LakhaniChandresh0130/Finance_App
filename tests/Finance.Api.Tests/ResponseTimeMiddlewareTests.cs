using System.Diagnostics;
using Finance.Api.Middleware;
using Finance.Api.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Finance.Api.Tests;

public sealed class ResponseTimeMiddlewareTests
{
    [Fact]
    public async Task Adds_x_response_time_ms_and_server_timing_when_response_writes_body()
    {
        var options = new PerformanceOptions
        {
            ExposeResponseTimeHeaders = true,
            SlowRequestWarningThresholdMs = 10_000,
            SlowRequestErrorThresholdMs = 20_000
        };
        var monitor = new FixedOptionsMonitor(options);

        RequestDelegate next = async ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            await ctx.Response.WriteAsync("ok");
        };

        var mw = new ResponseTimeMiddleware(next, monitor, NullLogger<ResponseTimeMiddleware>.Instance);
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();

        await mw.InvokeAsync(ctx);

        Assert.True(ctx.Response.Headers.ContainsKey("X-Response-Time-Ms"));
        var header = ctx.Response.Headers["X-Response-Time-Ms"].ToString();
        Assert.False(string.IsNullOrWhiteSpace(header));
        Assert.True(double.Parse(header, System.Globalization.CultureInfo.InvariantCulture) >= 0);

        Assert.True(ctx.Response.Headers.ContainsKey("Server-Timing"));
        Assert.Contains("app;dur=", ctx.Response.Headers["Server-Timing"].ToString());
    }

    [Fact]
    public async Task Delayed_pipeline_still_exposes_header_within_reasonable_budget_for_trivial_work()
    {
        var options = new PerformanceOptions
        {
            ExposeResponseTimeHeaders = true,
            SlowRequestWarningThresholdMs = 10_000,
            SlowRequestErrorThresholdMs = 20_000
        };
        var monitor = new FixedOptionsMonitor(options);

        RequestDelegate next = async ctx =>
        {
            await Task.Delay(15);
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            await ctx.Response.WriteAsync("done");
        };

        var mw = new ResponseTimeMiddleware(next, monitor, NullLogger<ResponseTimeMiddleware>.Instance);
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();

        var sw = Stopwatch.StartNew();
        await mw.InvokeAsync(ctx);
        sw.Stop();

        Assert.True(ctx.Response.Headers.ContainsKey("X-Response-Time-Ms"));
        var reported = double.Parse(ctx.Response.Headers["X-Response-Time-Ms"].ToString()!, System.Globalization.CultureInfo.InvariantCulture);
        Assert.InRange(reported, 10, 500);
        Assert.InRange(sw.ElapsedMilliseconds, 10, 500);
    }

    private sealed class FixedOptionsMonitor : IOptionsMonitor<PerformanceOptions>
    {
        public FixedOptionsMonitor(PerformanceOptions current) => CurrentValue = current;
        public PerformanceOptions CurrentValue { get; }
        public PerformanceOptions Get(string? name) => CurrentValue;
        public IDisposable OnChange(Action<PerformanceOptions, string?> listener) => NullChange.Instance;

        private sealed class NullChange : IDisposable
        {
            public static readonly NullChange Instance = new();
            public void Dispose() { }
        }
    }
}
