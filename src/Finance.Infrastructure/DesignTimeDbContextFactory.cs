using Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Finance.Infrastructure;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FinanceDbContext>
{
    public FinanceDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("FINANCE_DB")
            ?? "Host=localhost;Port=5432;Database=finance_db;Username=postgres;Password=123456";

        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseNpgsql(conn)
            .Options;

        return new FinanceDbContext(options);
    }
}
