using Finance.Application.Abstractions;
using Finance.Application.Common;
using Finance.Application.Records;
using Finance.Domain.Entities;
using Finance.Domain.Enums;

namespace Finance.Application.Services;

public sealed class FinancialRecordService
{
    private readonly IFinancialRecordRepository _records;
    private readonly IUserRepository _users;

    public FinancialRecordService(IFinancialRecordRepository records, IUserRepository users)
    {
        _records = records;
        _users = users;
    }

    public async Task<Result<FinancialRecordResponse>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _records.GetByIdAsync(id, ct);
        return entity is null || entity.IsDeleted
            ? Result<FinancialRecordResponse>.Fail("Record not found.")
            : Result<FinancialRecordResponse>.Ok(Map(entity));
    }

    public async Task<PagedResult<FinancialRecordResponse>> GetPagedAsync(
        RecordQueryParameters parameters,
        CancellationToken ct = default)
    {
        var (page, size) = parameters.Normalize();
        var (items, total) = await _records.GetPagedAsync(
            page,
            size,
            parameters.Type,
            parameters.Category,
            parameters.From,
            parameters.To,
            parameters.Search,
            ct);

        var mapped = items.Select(Map).ToList();
        return PagedResult<FinancialRecordResponse>.Create(mapped, page, size, total);
    }

    public async Task<Result<FinancialRecordResponse>> CreateAsync(
        CreateFinancialRecordRequest request,
        Guid createdByUserId,
        CancellationToken ct = default)
    {
        var entity = new FinancialRecord
        {
            Id = Guid.NewGuid(),
            Amount = request.Amount,
            Type = request.Type,
            Category = request.Category.Trim(),
            RecordDate = request.RecordDate,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false,
            Version = 1
        };

        await _records.AddAsync(entity, ct);
        var creatorEmail = await _users.GetEmailByIdAsync(createdByUserId, ct) ?? string.Empty;
        return Result<FinancialRecordResponse>.Ok(Map(entity, creatorEmail));
    }

    public async Task<BulkCreateFinancialRecordsResponse> CreateBulkAsync(
        IReadOnlyList<CreateFinancialRecordRequest> items,
        Guid createdByUserId,
        bool summaryOnly,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var entities = new List<FinancialRecord>(items.Count);
        foreach (var request in items)
        {
            entities.Add(new FinancialRecord
            {
                Id = Guid.NewGuid(),
                Amount = request.Amount,
                Type = request.Type,
                Category = request.Category.Trim(),
                RecordDate = request.RecordDate,
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                CreatedByUserId = createdByUserId,
                CreatedAtUtc = now,
                IsDeleted = false,
                Version = 1
            });
        }

        await _records.AddRangeAsync(entities, ct);
        var ids = entities.Select(e => e.Id).ToList();
        if (summaryOnly)
        {
            return new BulkCreateFinancialRecordsResponse
            {
                CreatedCount = entities.Count,
                CreatedIds = ids,
                Records = Array.Empty<FinancialRecordResponse>()
            };
        }

        var creatorEmail = await _users.GetEmailByIdAsync(createdByUserId, ct) ?? string.Empty;
        var mapped = entities.Select(e => Map(e, creatorEmail)).ToList();
        return new BulkCreateFinancialRecordsResponse
        {
            CreatedCount = mapped.Count,
            CreatedIds = ids,
            Records = mapped
        };
    }

    public async Task<Result<FinancialRecordResponse>> UpdateAsync(
        Guid id,
        UpdateFinancialRecordRequest request,
        CancellationToken ct = default)
    {
        var entity = await _records.GetByIdAsync(id, ct);
        if (entity is null || entity.IsDeleted)
            return Result<FinancialRecordResponse>.Fail("Record not found.");

        if (request.ExpectedVersion != entity.Version)
            return Result<FinancialRecordResponse>.Conflict(
                "The record was updated by another user. Refresh to load the latest version and try again.");

        if (request.Amount.HasValue) entity.Amount = request.Amount.Value;
        if (request.Type.HasValue) entity.Type = request.Type.Value;
        if (request.Category is not null) entity.Category = request.Category.Trim();
        if (request.RecordDate.HasValue) entity.RecordDate = request.RecordDate.Value;
        if (request.Notes is not null) entity.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

        entity.UpdatedAtUtc = DateTime.UtcNow;
        entity.Version++;
        try
        {
            await _records.UpdateAsync(entity, ct);
        }
        catch (ConcurrencyConflictException)
        {
            return Result<FinancialRecordResponse>.Conflict(
                "The record was modified concurrently. Refresh and retry.");
        }

        return Result<FinancialRecordResponse>.Ok(Map(entity));
    }

    public async Task<Result> SoftDeleteAsync(Guid id, int expectedVersion, CancellationToken ct = default)
    {
        var entity = await _records.GetByIdAsync(id, ct);
        if (entity is null || entity.IsDeleted)
            return Result.Fail("Record not found.");

        if (expectedVersion != entity.Version)
            return Result.Conflict(
                "The record was updated by another user. Refresh to load the latest version and try again.");

        entity.IsDeleted = true;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        entity.Version++;
        try
        {
            await _records.UpdateAsync(entity, ct);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Conflict("The record was modified concurrently. Refresh and retry.");
        }

        return Result.Ok();
    }

    private static FinancialRecordResponse Map(FinancialRecord r) =>
        Map(r, r.CreatedBy?.Email ?? string.Empty);

    private static FinancialRecordResponse Map(FinancialRecord r, string createdByEmail) => new()
    {
        Id = r.Id,
        Amount = r.Amount,
        Type = r.Type.ToString(),
        Category = r.Category,
        RecordDate = r.RecordDate,
        Notes = r.Notes,
        CreatedByUserId = r.CreatedByUserId,
        CreatedByEmail = createdByEmail,
        CreatedAtUtc = r.CreatedAtUtc,
        UpdatedAtUtc = r.UpdatedAtUtc,
        Version = r.Version
    };
}
