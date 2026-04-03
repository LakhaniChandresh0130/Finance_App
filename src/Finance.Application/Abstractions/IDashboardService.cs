using Finance.Application.Dashboard;

namespace Finance.Application.Abstractions;

public interface IDashboardService
{
    Task<DashboardSummaryResponse> GetSummaryAsync(
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default);
}
