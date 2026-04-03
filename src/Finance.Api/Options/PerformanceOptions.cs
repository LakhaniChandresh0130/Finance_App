namespace Finance.Api.Options;

/// <summary>
/// Tunable thresholds aligned with common API latency expectations (auth/dashboard vs reporting).
/// </summary>
public sealed class PerformanceOptions
{
    public const string SectionName = "Performance";

    /// <summary>Log Warning when total server time exceeds this (ms). Default 800 (upper "good" band).</summary>
    public int SlowRequestWarningThresholdMs { get; set; } = 800;

    /// <summary>Log Error when total server time exceeds this (ms). Default 2000.</summary>
    public int SlowRequestErrorThresholdMs { get; set; } = 2000;

    /// <summary>Paths where slow-request logging is suppressed (e.g. health probes that include DB checks).</summary>
    public string[] ExcludedPathsFromSlowLogging { get; set; } = ["/health"];

    /// <summary>Expose X-Response-Time-Ms header (and Server-Timing app;dur=).</summary>
    public bool ExposeResponseTimeHeaders { get; set; } = true;

    /// <summary>Also used by Infrastructure dashboard cache (same JSON section). 0 disables cache.</summary>
    public int DashboardSummaryCacheSeconds { get; set; } = 30;
}
