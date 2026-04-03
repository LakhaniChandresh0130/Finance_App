using Finance.Application.Abstractions;
using Finance.Application.Common;
using Finance.Application.Users;
using Finance.Domain.Entities;

namespace Finance.Application.Services;

public sealed class UserAdminService
{
    private readonly IUserRepository _users;
    private readonly IPasswordProtector _passwords;

    public UserAdminService(IUserRepository users, IPasswordProtector passwords)
    {
        _users = users;
        _passwords = passwords;
    }

    public async Task<Result<UserResponse>> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        if (await _users.EmailExistsAsync(request.Email.Trim().ToLowerInvariant(), ct))
            return Result<UserResponse>.Fail("A user with this email already exists.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = _passwords.Hash(request.Password),
            FullName = request.FullName.Trim(),
            Role = request.Role,
            IsActive = request.IsActive,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _users.AddAsync(user, ct);
        return Result<UserResponse>.Ok(Map(user));
    }

    public async Task<Result<UserResponse>> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(id, ct);
        if (user is null)
            return Result<UserResponse>.Fail("User not found.");

        if (request.FullName is not null) user.FullName = request.FullName.Trim();
        if (request.Role.HasValue) user.Role = request.Role.Value;
        if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;

        await _users.UpdateAsync(user, ct);
        return Result<UserResponse>.Ok(Map(user));
    }

    public async Task<Result<UserResponse>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(id, ct);
        return user is null
            ? Result<UserResponse>.Fail("User not found.")
            : Result<UserResponse>.Ok(Map(user));
    }

    public async Task<PagedResult<UserResponse>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? search,
        bool? isActive,
        Domain.Enums.UserRole? role,
        CancellationToken ct = default)
    {
        var page = pageNumber < 1 ? 1 : pageNumber;
        var size = pageSize switch { < 1 => 10, > 100 => 100, _ => pageSize };
        var (items, total) = await _users.GetPagedAsync(page, size, search, isActive, role, ct);
        var mapped = items.Select(Map).ToList();
        return PagedResult<UserResponse>.Create(mapped, page, size, total);
    }

    private static UserResponse Map(User u) => new()
    {
        Id = u.Id,
        Email = u.Email,
        FullName = u.FullName,
        Role = u.Role.ToString(),
        IsActive = u.IsActive,
        CreatedAtUtc = u.CreatedAtUtc
    };
}
