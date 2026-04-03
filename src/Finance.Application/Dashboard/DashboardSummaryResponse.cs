namespace Finance.Application.Dashboard;

public sealed class DashboardSummaryResponse
{
    public decimal TotalIncome { get; init; }
    public decimal TotalExpenses { get; init; }
    public decimal NetBalance { get; init; }
    public IReadOnlyList<CategoryTotalDto> CategoryTotals { get; init; } = Array.Empty<CategoryTotalDto>();
    public IReadOnlyList<RecentActivityDto> RecentActivity { get; init; } = Array.Empty<RecentActivityDto>();
    public IReadOnlyList<MonthlyTrendDto> MonthlyTrends { get; init; } = Array.Empty<MonthlyTrendDto>();
}

public sealed class CategoryTotalDto
{
    public string Category { get; init; } = string.Empty;
    public decimal TotalIncome { get; init; }
    public decimal TotalExpense { get; init; }
}

public sealed class RecentActivityDto
{
    public Guid Id { get; init; }
    public decimal Amount { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public DateOnly RecordDate { get; init; }
}

public sealed class MonthlyTrendDto
{
    public int Year { get; init; }
    public int Month { get; init; }
    public decimal Income { get; init; }
    public decimal Expense { get; init; }
}
