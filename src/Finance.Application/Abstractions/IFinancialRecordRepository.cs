using Finance.Domain.Entities;
using Finance.Domain.Enums;

namespace Finance.Application.Abstractions;

public interface IFinancialRecordRepository
{
    Task<FinancialRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(FinancialRecord record, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IReadOnlyList<FinancialRecord> records, CancellationToken cancellationToken = default);
    Task UpdateAsync(FinancialRecord record, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<FinancialRecord> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        TransactionType? type,
        string? category,
        DateOnly? from,
        DateOnly? to,
        string? search,
        CancellationToken cancellationToken = default);
}
