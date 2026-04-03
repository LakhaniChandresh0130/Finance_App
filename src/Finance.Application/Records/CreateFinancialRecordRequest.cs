using Finance.Domain.Enums;

namespace Finance.Application.Records;

public sealed class CreateFinancialRecordRequest
{
    public decimal Amount { get; init; }
    public TransactionType Type { get; init; }
    public string Category { get; init; } = string.Empty;
    public DateOnly RecordDate { get; init; }
    public string? Notes { get; init; }
}
