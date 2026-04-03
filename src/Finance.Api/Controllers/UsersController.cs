using Finance.Api.Authorization;
using Finance.Application.Services;
using Finance.Application.Users;
using Finance.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finance.Api.Controllers;

[ApiController]
[Authorize(Policy = PolicyNames.UsersAdmin)]
[Route("api/[controller]")]
public sealed class UsersController : ControllerBase
{
    private readonly UserAdminService _users;
    private readonly IValidator<CreateUserRequest> _createValidator;

    public UsersController(UserAdminService users, IValidator<CreateUserRequest> createValidator)
    {
        _users = users;
        _createValidator = createValidator;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] UserRole? role = null,
        CancellationToken cancellationToken = default)
    {
        var page = await _users.GetPagedAsync(pageNumber, pageSize, search, isActive, role, cancellationToken);
        return Ok(page);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _users.GetByIdAsync(id, cancellationToken);
        return result.Success ? Ok(result.Value) : NotFound(new { errors = result.Errors });
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            foreach (var e in validation.Errors)
                ModelState.AddModelError(e.PropertyName, e.ErrorMessage);
            return ValidationProblem(ModelState);
        }

        var result = await _users.CreateAsync(request, cancellationToken);
        if (!result.Success)
            return Conflict(new { errors = result.Errors });

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await _users.UpdateAsync(id, request, cancellationToken);
        return result.Success ? Ok(result.Value) : NotFound(new { errors = result.Errors });
    }
}
