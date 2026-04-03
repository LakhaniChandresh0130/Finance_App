using Finance.Application.Abstractions;
using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Finance.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(FinanceDbContext db, IPasswordProtector passwords, ILogger logger, CancellationToken cancellationToken)
    {
        if (await db.Users.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Database already seeded.");
            return;
        }

        var users = new List<User>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Email = "admin@finance.local",
                FullName = "System Admin",
                Role = UserRole.Admin,
                IsActive = true,
                PasswordHash = passwords.Hash("Admin123!"),
                CreatedAtUtc = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Email = "analyst@finance.local",
                FullName = "Demo Analyst",
                Role = UserRole.Analyst,
                IsActive = true,
                PasswordHash = passwords.Hash("Analyst123!"),
                CreatedAtUtc = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Email = "viewer@finance.local",
                FullName = "Demo Viewer",
                Role = UserRole.Viewer,
                IsActive = true,
                PasswordHash = passwords.Hash("Viewer123!"),
                CreatedAtUtc = DateTime.UtcNow
            }
        };

        db.Users.AddRange(users);
        await db.SaveChangesAsync(cancellationToken);

        var adminId = users[0].Id;
        var demoRecords = new List<FinancialRecord>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Amount = 5000m,
                Type = TransactionType.Income,
                Category = "Salary",
                RecordDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
                Notes = "Sample income",
                CreatedByUserId = adminId,
                CreatedAtUtc = DateTime.UtcNow,
                IsDeleted = false,
                Version = 1
            },
            new()
            {
                Id = Guid.NewGuid(),
                Amount = 120.50m,
                Type = TransactionType.Expense,
                Category = "Utilities",
                RecordDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
                Notes = "Electricity",
                CreatedByUserId = adminId,
                CreatedAtUtc = DateTime.UtcNow,
                IsDeleted = false,
                Version = 1
            }
        };

        db.FinancialRecords.AddRange(demoRecords);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Database seed completed (demo users + sample records).");
    }
}
