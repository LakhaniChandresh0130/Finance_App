using System.Reflection;
using FluentValidation;
using Finance.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Finance.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddScoped<FinancialRecordService>();
        services.AddScoped<UserAdminService>();
        return services;
    }
}
