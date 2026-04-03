using Finance.Domain.Enums;

namespace Finance.Domain.Entities;

public class FinancialRecord
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string Category { get; set; } = string.Empty;
    public DateOnly RecordDate { get; set; }
    public string? Notes { get; set; }
    public Guid CreatedByUserId { get; set; }
    public User CreatedBy { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public bool IsDeleted { get; set; }

    /// <summary>Optimistic concurrency token; incremented on each successful update.</summary>
    public int Version { get; set; } = 1;
}
