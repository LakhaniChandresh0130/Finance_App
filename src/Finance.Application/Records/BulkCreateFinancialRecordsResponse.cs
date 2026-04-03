namespace Finance.Application.Records;

public sealed class BulkCreateFinancialRecordsResponse
{
    public int CreatedCount { get; init; }
    /// <summary>Populated when <c>summaryOnly</c>; use for faster responses (no large JSON body).</summary>
    public IReadOnlyList<Guid>? CreatedIds { get; init; }
    public IReadOnlyList<FinancialRecordResponse> Records { get; init; } = Array.Empty<FinancialRecordResponse>();
}
