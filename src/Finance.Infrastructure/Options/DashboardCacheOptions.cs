namespace Finance.Infrastructure.Options;

/// <summary>Binds to the same <c>Performance</c> configuration section as the API host (cache-related subset).</summary>
public sealed class DashboardCacheOptions
{
    public const string SectionName = "Performance";

    /// <summary>Seconds to cache GET dashboard summary per date range. 0 disables caching.</summary>
    public int DashboardSummaryCacheSeconds { get; set; } = 30;
}
