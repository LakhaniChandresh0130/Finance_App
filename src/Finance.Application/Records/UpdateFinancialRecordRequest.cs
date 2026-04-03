using Finance.Domain.Enums;

namespace Finance.Application.Records;

public sealed class UpdateFinancialRecordRequest
{
    /// <summary>Version from the last GET (or list); must match current row or update is rejected with 409.</summary>
    public int ExpectedVersion { get; init; }

    public decimal? Amount { get; init; }
    public TransactionType? Type { get; init; }
    public string? Category { get; init; }
    public DateOnly? RecordDate { get; init; }
    public string? Notes { get; init; }
}
