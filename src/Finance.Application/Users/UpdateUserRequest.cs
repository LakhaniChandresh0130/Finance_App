using Finance.Domain.Enums;

namespace Finance.Application.Users;

public sealed class UpdateUserRequest
{
    public string? FullName { get; init; }
    public UserRole? Role { get; init; }
    public bool? IsActive { get; init; }
}
