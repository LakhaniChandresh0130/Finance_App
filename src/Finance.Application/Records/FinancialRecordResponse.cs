namespace Finance.Application.Records;

public sealed class FinancialRecordResponse
{
    public Guid Id { get; init; }
    public decimal Amount { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public DateOnly RecordDate { get; init; }
    public string? Notes { get; init; }
    public Guid CreatedByUserId { get; init; }
    public string CreatedByEmail { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
    /// <summary>Send as <c>expectedVersion</c> on PUT/DELETE to detect concurrent edits.</summary>
    public int Version { get; init; }
}
