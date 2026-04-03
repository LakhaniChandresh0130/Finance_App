using Finance.Application.Abstractions;
using Finance.Application.Auth;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Finance.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly IValidator<LoginRequest> _validator;

    public AuthController(IAuthService auth, IValidator<LoginRequest> validator)
    {
        _auth = auth;
        _validator = validator;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            foreach (var e in validation.Errors)
                ModelState.AddModelError(e.PropertyName, e.ErrorMessage);
            return ValidationProblem(ModelState);
        }

        var response = await _auth.LoginAsync(request, cancellationToken);
        return response is null
            ? Unauthorized(new ProblemDetails { Title = "Invalid credentials", Status = StatusCodes.Status401Unauthorized })
            : Ok(response);
    }
}
