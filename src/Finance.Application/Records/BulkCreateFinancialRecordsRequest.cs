namespace Finance.Application.Records;

/// <summary>Admin bulk insert: up to <see cref="BulkCreateFinancialRecordsLimits.MaxItems"/> rows per request.</summary>
public sealed class BulkCreateFinancialRecordsRequest
{
    public IReadOnlyList<CreateFinancialRecordRequest> Items { get; init; } = Array.Empty<CreateFinancialRecordRequest>();
}

public static class BulkCreateFinancialRecordsLimits
{
    public const int MaxItems = 100;
}
