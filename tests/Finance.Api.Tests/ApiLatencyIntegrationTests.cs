using System.Diagnostics;
using System.Net.Http.Json;
using Finance.Application.Auth;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Finance.Api.Tests;

/// <summary>
/// End-to-end timing checks against real infrastructure (PostgreSQL + migrations + seed).
/// Set <c>FINANCE_PERF_TEST=1</c> and a reachable connection string to run; otherwise these exit early.
/// Budgets follow typical expectations: auth ~400ms, health ~600ms, dashboard ~800ms for warm, small datasets (local).
/// </summary>
public sealed class ApiLatencyIntegrationTests
{
    private const string PerfEnv = "FINANCE_PERF_TEST";

    [Fact]
    public async Task When_perf_env_enabled_login_and_dashboard_meet_headers_and_soft_budgets()
    {
        if (Environment.GetEnvironmentVariable(PerfEnv) != "1")
            return;

        await using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        var swLogin = Stopwatch.StartNew();
        var loginRes = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest { Email = "admin@finance.local", Password = "Admin123!" });
        swLogin.Stop();

        Assert.True(loginRes.IsSuccessStatusCode, await loginRes.Content.ReadAsStringAsync());
        Assert.True(loginRes.Headers.Contains("X-Response-Time-Ms"), "Login should expose X-Response-Time-Ms for observability.");
        Assert.True(swLogin.ElapsedMilliseconds < 2000, $"Auth wall time {swLogin.ElapsedMilliseconds}ms (ideal under ~400ms; tolerance 2s for dev/CI).");

        var hdrLogin = double.Parse(loginRes.Headers.GetValues("X-Response-Time-Ms").First(), System.Globalization.CultureInfo.InvariantCulture);
        Assert.True(hdrLogin < 2000, $"Server-reported login {hdrLogin}ms should stay under error budget.");

        var body = await loginRes.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", body.AccessToken);

        var swDash = Stopwatch.StartNew();
        var dashRes = await client.GetAsync("/api/dashboard/summary");
        swDash.Stop();
        Assert.True(dashRes.IsSuccessStatusCode, await dashRes.Content.ReadAsStringAsync());
        Assert.True(dashRes.Headers.Contains("X-Response-Time-Ms"));
        Assert.True(swDash.ElapsedMilliseconds < 3000, $"Dashboard wall time {swDash.ElapsedMilliseconds}ms (reporting-tier workloads often 1–3s; cap 3s here for dev).");

        var swDash2 = Stopwatch.StartNew();
        var dashRes2 = await client.GetAsync("/api/dashboard/summary");
        swDash2.Stop();
        Assert.True(dashRes2.IsSuccessStatusCode);
        Assert.True(swDash2.ElapsedMilliseconds < swDash.ElapsedMilliseconds + 100,
            "Second dashboard call should benefit from memory cache (typically much faster).");

        var swHealth = Stopwatch.StartNew();
        var health = await client.GetAsync("/health");
        swHealth.Stop();
        Assert.True(health.Headers.Contains("X-Response-Time-Ms"));
        Assert.True(swHealth.ElapsedMilliseconds < 2500, $"Health check {swHealth.ElapsedMilliseconds}ms (includes DB check; relax on cold pool).");
    }
}
