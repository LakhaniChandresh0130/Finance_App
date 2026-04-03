using Finance.Api.Authorization;
using Finance.Api.Extensions;
using Finance.Application.Common;
using Finance.Application.Records;
using Finance.Application.Services;
using Finance.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finance.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class RecordsController : ControllerBase
{
    private readonly FinancialRecordService _records;
    private readonly IValidator<CreateFinancialRecordRequest> _createValidator;
    private readonly IValidator<UpdateFinancialRecordRequest> _updateValidator;
    private readonly IValidator<BulkCreateFinancialRecordsRequest> _bulkValidator;

    public RecordsController(
        FinancialRecordService records,
        IValidator<CreateFinancialRecordRequest> createValidator,
        IValidator<UpdateFinancialRecordRequest> updateValidator,
        IValidator<BulkCreateFinancialRecordsRequest> bulkValidator)
    {
        _records = records;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _bulkValidator = bulkValidator;
    }

    [Authorize(Policy = PolicyNames.RecordsRead)]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<FinancialRecordResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] TransactionType? type = null,
        [FromQuery] string? category = null,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = new RecordQueryParameters
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Type = type,
            Category = category,
            From = from,
            To = to,
            Search = search
        };

        var page = await _records.GetPagedAsync(query, cancellationToken);
        return Ok(page);
    }

    [Authorize(Policy = PolicyNames.RecordsRead)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FinancialRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _records.GetByIdAsync(id, cancellationToken);
        return result.Success ? Ok(result.Value) : NotFound(new { errors = result.Errors });
    }

    [Authorize(Policy = PolicyNames.RecordsWrite)]
    [HttpPost]
    [ProducesResponseType(typeof(FinancialRecordResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateFinancialRecordRequest request, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            foreach (var e in validation.Errors)
                ModelState.AddModelError(e.PropertyName, e.ErrorMessage);
            return ValidationProblem(ModelState);
        }

        var userId = User.GetUserId();
        var result = await _records.CreateAsync(request, userId, cancellationToken);
        if (!result.Success)
            return BadRequest(new { errors = result.Errors });

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>Create up to 100 financial records in one request (same rules as single POST).</summary>
    /// <param name="summaryOnly">If true, returns only <c>createdCount</c> and <c>createdIds</c> (faster, smaller payload).</param>
    [Authorize(Policy = PolicyNames.RecordsWrite)]
    [HttpPost("batch")]
    [ProducesResponseType(typeof(BulkCreateFinancialRecordsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BatchCreate(
        [FromBody] BulkCreateFinancialRecordsRequest request,
        [FromQuery] bool summaryOnly = false,
        CancellationToken cancellationToken = default)
    {
        var validation = await _bulkValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            foreach (var e in validation.Errors)
                ModelState.AddModelError(e.PropertyName, e.ErrorMessage);
            return ValidationProblem(ModelState);
        }

        var userId = User.GetUserId();
        var result = await _records.CreateBulkAsync(request.Items, userId, summaryOnly, cancellationToken);
        return Ok(result);
    }

    [Authorize(Policy = PolicyNames.RecordsWrite)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(FinancialRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFinancialRecordRequest request, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            foreach (var e in validation.Errors)
                ModelState.AddModelError(e.PropertyName, e.ErrorMessage);
            return ValidationProblem(ModelState);
        }

        var result = await _records.UpdateAsync(id, request, cancellationToken);
        if (result.Success)
            return Ok(result.Value);
        if (result.IsConcurrencyConflict)
            return Conflict(new { errors = result.Errors, code = "CONCURRENCY_CONFLICT" });
        return NotFound(new { errors = result.Errors });
    }

    [Authorize(Policy = PolicyNames.RecordsWrite)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromQuery] int expectedVersion,
        CancellationToken cancellationToken)
    {
        if (expectedVersion <= 0)
        {
            ModelState.AddModelError(nameof(expectedVersion),
                "Must be greater than 0 (copy Version from the latest GET /api/records/{id}).");
            return ValidationProblem(ModelState);
        }

        var result = await _records.SoftDeleteAsync(id, expectedVersion, cancellationToken);
        if (result.Success)
            return NoContent();
        if (result.IsConcurrencyConflict)
            return Conflict(new { errors = result.Errors, code = "CONCURRENCY_CONFLICT" });
        return NotFound(new { errors = result.Errors });
    }
}
