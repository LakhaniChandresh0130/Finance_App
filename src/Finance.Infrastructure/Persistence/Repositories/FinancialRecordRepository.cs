using Finance.Application.Abstractions;
using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Finance.Infrastructure.Persistence.Repositories;

internal sealed class FinancialRecordRepository : IFinancialRecordRepository
{
    private readonly FinanceDbContext _db;

    public FinancialRecordRepository(FinanceDbContext db)
    {
        _db = db;
    }

    public Task<FinancialRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.FinancialRecords
            .Include(x => x.CreatedBy)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddAsync(FinancialRecord record, CancellationToken cancellationToken = default)
    {
        _db.FinancialRecords.Add(record);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IReadOnlyList<FinancialRecord> records, CancellationToken cancellationToken = default)
    {
        if (records.Count == 0)
            return;

        _db.FinancialRecords.AddRange(records);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(FinancialRecord record, CancellationToken cancellationToken = default)
    {
        _db.FinancialRecords.Update(record);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException(
                "The record was changed by another user. Refresh and try again.");
        }
    }

    public async Task<(IReadOnlyList<FinancialRecord> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        TransactionType? type,
        string? category,
        DateOnly? from,
        DateOnly? to,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = _db.FinancialRecords.AsNoTracking().Where(x => !x.IsDeleted);

        if (type.HasValue)
            query = query.Where(x => x.Type == type.Value);

        if (!string.IsNullOrWhiteSpace(category))
        {
            var c = category.Trim();
            query = query.Where(x => x.Category == c);
        }

        if (from.HasValue)
            query = query.Where(x => x.RecordDate >= from.Value);

        if (to.HasValue)
            query = query.Where(x => x.RecordDate <= to.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                EF.Functions.ILike(x.Category, $"%{term}%") ||
                (x.Notes != null && EF.Functions.ILike(x.Notes, $"%{term}%")));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .Include(x => x.CreatedBy)
            .OrderByDescending(x => x.RecordDate)
            .ThenByDescending(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
