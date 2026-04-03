using Finance.Domain.Enums;

namespace Finance.Application.Records;

public sealed class RecordQueryParameters
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 100;

    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = DefaultPageSize;
    public TransactionType? Type { get; init; }
    public string? Category { get; init; }
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }
    public string? Search { get; init; }

    public (int PageNumber, int PageSize) Normalize()
    {
        var page = PageNumber < 1 ? 1 : PageNumber;
        var size = PageSize switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => PageSize
        };
        return (page, size);
    }
}
