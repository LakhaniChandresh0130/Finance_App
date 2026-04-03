using Finance.Application.Abstractions;
using Finance.Application.Auth;
using Microsoft.Extensions.Options;

namespace Finance.Infrastructure.Security;

internal sealed class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordProtector _passwords;
    private readonly ITokenService _tokens;
    private readonly JwtOptions _jwt;

    public AuthService(
        IUserRepository users,
        IPasswordProtector passwords,
        ITokenService tokens,
        IOptions<JwtOptions> jwt)
    {
        _users = users;
        _passwords = passwords;
        _tokens = tokens;
        _jwt = jwt.Value;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _users.GetByEmailAsync(email, cancellationToken);
        if (user is null || !user.IsActive)
            return null;

        if (_passwords.VerifyHashedPassword(user.PasswordHash, request.Password) != PasswordVerificationOutcome.Success)
            return null;

        var token = _tokens.CreateToken(user);
        var expires = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes);

        return new LoginResponse
        {
            AccessToken = token,
            ExpiresAtUtc = expires,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role.ToString()
        };
    }
}
