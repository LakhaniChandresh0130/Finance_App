using Finance.Application.Abstractions;
using Finance.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Finance.Infrastructure.Security;

internal sealed class PasswordProtectorAdapter : IPasswordProtector
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(string password) =>
        _hasher.HashPassword(new User(), password);

    public PasswordVerificationOutcome VerifyHashedPassword(string hashedPassword, string providedPassword)
    {
        var result = _hasher.VerifyHashedPassword(new User(), hashedPassword, providedPassword);
        return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded
            ? PasswordVerificationOutcome.Success
            : PasswordVerificationOutcome.Failed;
    }
}
