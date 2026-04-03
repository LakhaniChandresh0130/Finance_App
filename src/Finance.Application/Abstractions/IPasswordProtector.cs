namespace Finance.Application.Abstractions;

public enum PasswordVerificationOutcome
{
    Failed,
    Success
}

public interface IPasswordProtector
{
    string Hash(string password);
    PasswordVerificationOutcome VerifyHashedPassword(string hashedPassword, string providedPassword);
}
