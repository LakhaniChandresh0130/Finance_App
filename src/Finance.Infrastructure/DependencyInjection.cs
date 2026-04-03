using Finance.Application.Abstractions;
using Finance.Infrastructure.Dashboard;
using Finance.Infrastructure.Options;
using Finance.Infrastructure.Persistence;
using Finance.Infrastructure.Persistence.Repositories;
using Finance.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Finance.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DashboardCacheOptions>(configuration.GetSection(DashboardCacheOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        var connectionString = configuration.GetConnectionString("FinanceDatabase")
            ?? throw new InvalidOperationException("Connection string 'FinanceDatabase' is not configured.");

        services.AddDbContext<FinanceDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                npgsql.CommandTimeout(30);
            });
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IFinancialRecordRepository, FinancialRecordRepository>();
        services.AddSingleton<IPasswordProtector, PasswordProtectorAdapter>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDashboardService, DashboardService>();

        return services;
    }
}
