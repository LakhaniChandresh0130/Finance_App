using Finance.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Finance.Api.Authorization;

public static class PolicyNames
{
    public const string Dashboard = nameof(Dashboard);
    public const string RecordsRead = nameof(RecordsRead);
    public const string RecordsWrite = nameof(RecordsWrite);
    public const string UsersAdmin = nameof(UsersAdmin);

    public static void AddFinancePolicies(AuthorizationOptions options)
    {
        options.AddPolicy(Dashboard, p =>
            p.RequireRole(nameof(UserRole.Viewer), nameof(UserRole.Analyst), nameof(UserRole.Admin)));

        options.AddPolicy(RecordsRead, p =>
            p.RequireRole(nameof(UserRole.Analyst), nameof(UserRole.Admin)));

        options.AddPolicy(RecordsWrite, p =>
            p.RequireRole(nameof(UserRole.Admin)));

        options.AddPolicy(UsersAdmin, p =>
            p.RequireRole(nameof(UserRole.Admin)));
    }
}
