using Finance.Application.Abstractions;
using Finance.Application.Dashboard;
using Finance.Domain.Enums;
using Finance.Infrastructure.Options;
using Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Finance.Infrastructure.Dashboard;

/// <summary>
/// Aggregations use EF Core LINQ (parameterized SQL). Monthly trends use SqlQuery with bound parameters.
/// Optional memory cache reduces repeat load for dashboard-style traffic.
/// </summary>
internal sealed class DashboardService : IDashboardService
{
    private readonly FinanceDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly DashboardCacheOptions _cacheOptions;

    public DashboardService(
        FinanceDbContext db,
        IMemoryCache cache,
        IOptions<DashboardCacheOptions> cacheOptions)
    {
        _db = db;
        _cache = cache;
        _cacheOptions = cacheOptions.Value;
    }

    public Task<DashboardSummaryResponse> GetSummaryAsync(
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        var fromDate = from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1));
        var toDate = to ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var seconds = _cacheOptions.DashboardSummaryCacheSeconds;
        if (seconds <= 0)
            return ComputeSummaryAsync(fromDate, toDate, cancellationToken);

        var cacheKey = $"dashboard:v1:{fromDate:O}:{toDate:O}";
        return _cache.GetOrCreateAsync(
            cacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(seconds);
                return await ComputeSummaryAsync(fromDate, toDate, cancellationToken);
            })!;
    }

    private async Task<DashboardSummaryResponse> ComputeSummaryAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken)
    {
        var scoped = _db.FinancialRecords.AsNoTracking()
            .Where(x => !x.IsDeleted && x.RecordDate >= fromDate && x.RecordDate <= toDate);

        // Single round-trip for both income and expense totals
        var totalsByType = await scoped
            .GroupBy(x => x.Type)
            .Select(g => new { Type = g.Key, Total = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);

        var totalIncome = totalsByType.FirstOrDefault(t => t.Type == TransactionType.Income)?.Total ?? 0m;
        var totalExpenses = totalsByType.FirstOrDefault(t => t.Type == TransactionType.Expense)?.Total ?? 0m;

        var categoryTotals = await scoped
            .GroupBy(x => x.Category)
            .Select(g => new CategoryTotalDto
            {
                Category = g.Key,
                TotalIncome = g.Where(x => x.Type == TransactionType.Income).Sum(x => x.Amount),
                TotalExpense = g.Where(x => x.Type == TransactionType.Expense).Sum(x => x.Amount)
            })
            .OrderByDescending(x => x.TotalIncome + x.TotalExpense)
            .ToListAsync(cancellationToken);

        var recentActivity = await scoped
            .OrderByDescending(x => x.RecordDate)
            .ThenByDescending(x => x.Id)
            .Take(10)
            .Select(x => new RecentActivityDto
            {
                Id = x.Id,
                Amount = x.Amount,
                Type = x.Type.ToString(),
                Category = x.Category,
                RecordDate = x.RecordDate
            })
            .ToListAsync(cancellationToken);

        var monthlyTrends = await _db.Database
            .SqlQuery<MonthlyTrendDto>($"""
                SELECT
                    CAST(date_part('year', r."RecordDate") AS integer) AS "Year",
                    CAST(date_part('month', r."RecordDate") AS integer) AS "Month",
                    COALESCE(SUM(CASE WHEN r."Type" = {(int)TransactionType.Income} THEN r."Amount" ELSE 0 END), 0) AS "Income",
                    COALESCE(SUM(CASE WHEN r."Type" = {(int)TransactionType.Expense} THEN r."Amount" ELSE 0 END), 0) AS "Expense"
                FROM financial_records AS r
                WHERE r."IsDeleted" = false
                  AND r."RecordDate" >= {fromDate}
                  AND r."RecordDate" <= {toDate}
                GROUP BY 1, 2
                ORDER BY 1 ASC, 2 ASC
                """)
            .ToListAsync(cancellationToken);

        return new DashboardSummaryResponse
        {
            TotalIncome = totalIncome,
            TotalExpenses = totalExpenses,
            NetBalance = totalIncome - totalExpenses,
            CategoryTotals = categoryTotals,
            RecentActivity = recentActivity,
            MonthlyTrends = monthlyTrends
        };
    }
}
