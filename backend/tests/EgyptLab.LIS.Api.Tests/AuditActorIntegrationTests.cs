using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EgyptLab.LIS.Api.Identity;
using EgyptLab.LIS.Infrastructure.Domain;
using EgyptLab.LIS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EgyptLab.LIS.Api.Tests;

/// <summary>
/// Exercises the real authentication pipeline and the audit actor middleware.
/// The probe endpoint is registered only by the test host and writes through
/// the same LisDbContext used by production endpoints.
/// </summary>
public sealed class AuditActorIntegrationTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public AuditActorIntegrationTests(AuthTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AuthenticatedRequest_WritesAuditRowWithActorFromValidatedToken()
    {
        var patientId = Guid.NewGuid();
        var oldPhone = "01000000000";
        var newPhone = "01111111111";

        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<LisDbContext>();
            db.Patients.Add(new Patient
            {
                Id = patientId,
                BranchId = _factory.BranchAId,
                FirstName = "Synthetic",
                LastName = "Audit-Patient",
                NationalId = $"SYN-{Guid.NewGuid():N}"[..20],
                Phone = oldPhone,
                DateOfBirth = new DateOnly(1990, 1, 1),
                Sex = "F"
            });
            await db.SaveChangesAsync();
        }

        using var probeFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
                services.AddSingleton<IStartupFilter, AuditProbeStartupFilter>());
        });

        var client = probeFactory.CreateClient();
        var login = await client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest("MAIN", "manager.a", "Manager@12345"));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var payload = await login.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(payload);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", payload.Token);

        // Untrusted request headers must not influence the audit actor.
        client.DefaultRequestHeaders.Add("X-Actor-UserId", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Actor-Branch", Guid.NewGuid().ToString());

        var response = await client.PostAsync(
            $"/__test/audit/patient?patientId={patientId}&phone={newPhone}",
            content: null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var verifyScope = _factory.Services.CreateScope();
        var branchScope = verifyScope.ServiceProvider.GetRequiredService<IBranchScope>();
        branchScope.Set(_factory.BranchAId);
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<LisDbContext>();

        var audit = await verifyDb.AuditLog.SingleAsync(
            item => item.EntityId == patientId && item.Action == "update");

        Assert.Equal(_factory.ManagerAUserId, audit.ActorUserId);
        Assert.Equal(_factory.BranchAId, audit.BranchId);
        Assert.Null(audit.ActorRole);
        Assert.Equal("127.0.0.1", audit.IpAddress);

        using var before = JsonDocument.Parse(audit.BeforeJson!);
        using var after = JsonDocument.Parse(audit.AfterJson!);
        Assert.Equal("[redacted]", before.RootElement.GetProperty("Phone").GetString());
        Assert.Equal("[redacted]", after.RootElement.GetProperty("Phone").GetString());
        Assert.DoesNotContain(oldPhone, audit.BeforeJson!, StringComparison.Ordinal);
        Assert.DoesNotContain(newPhone, audit.AfterJson!, StringComparison.Ordinal);

        branchScope.Clear();
    }

    [Fact]
    public async Task Login_DoesNotCreateActorOrUserAuditRow()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest("MAIN", "reception.a", "Reception@12345"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var branchScope = scope.ServiceProvider.GetRequiredService<IBranchScope>();
        branchScope.Set(_factory.BranchAId);
        var db = scope.ServiceProvider.GetRequiredService<LisDbContext>();

        Assert.Empty(await db.AuditLog
            .Where(item => item.EntityName == nameof(User))
            .ToListAsync());

        branchScope.Clear();
    }
}

/// <summary>
/// Adds a test-only mutation path after the production authentication, audit,
/// and branch-scope middleware. This lets the integration test use a real
/// login token without adding a test endpoint to the production API.
/// </summary>
internal sealed class AuditProbeStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        // TestServer does not set a remote address. Set a loopback value so the
        // audit actor middleware has an IP to persist, as it would in Kestrel.
        app.Use(async (context, nextMiddleware) =>
        {
            context.Connection.RemoteIpAddress ??= System.Net.IPAddress.Loopback;
            await nextMiddleware(context);
        });

        next(app);

        app.Use(async (context, nextMiddleware) =>
        {
            if (!HttpMethods.IsPost(context.Request.Method)
                || !context.Request.Path.Equals(
                    "/__test/audit/patient",
                    StringComparison.OrdinalIgnoreCase))
            {
                await nextMiddleware(context);
                return;
            }

            if (!Guid.TryParse(context.Request.Query["patientId"], out var patientId))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var db = context.RequestServices.GetRequiredService<LisDbContext>();
            var patient = await db.Patients.SingleOrDefaultAsync(item => item.Id == patientId);
            if (patient is null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            patient.Phone = context.Request.Query["phone"].ToString();
            await db.SaveChangesAsync();
            context.Response.StatusCode = StatusCodes.Status204NoContent;
        });
    };
}
