using Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Finance.Infrastructure;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FinanceDbContext>
{
    public FinanceDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("FINANCE_DB")
            ?? "postgresql://finance_2bkw_user:3AIJPHecXf2zQX8ZzVasQtIXP2IyWa6v@dpg-d77uf08gjchc73d6mm8g-a.oregon-postgres.render.com/finance_2bkw";

        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseNpgsql(conn)
            .Options;

        return new FinanceDbContext(options);
    }
}
