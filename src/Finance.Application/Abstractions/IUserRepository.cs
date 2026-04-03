using Finance.Domain.Entities;
using Finance.Domain.Enums;

namespace Finance.Application.Abstractions;

public interface IUserRepository
{
    /// <summary>Single column read for mapping creates without loading full user graph.</summary>
    Task<string?> GetEmailByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? search,
        bool? isActive,
        UserRole? role,
        CancellationToken cancellationToken = default);
}
