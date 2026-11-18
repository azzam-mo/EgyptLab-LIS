using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EgyptLab.LIS.Api.Identity;
using EgyptLab.LIS.Infrastructure.Domain;
using EgyptLab.LIS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EgyptLab.LIS.Api.Tests;

public sealed class AuthenticationTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public AuthenticationTests(AuthTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WithValidBranchCredentials_ReturnsTokenAndPermissions()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest("MAIN", "manager.a", "Manager@12345"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload.Token));
        Assert.Equal(_factory.BranchAId, payload.BranchId);
        Assert.Contains(LisPermissions.AdminUsers, payload.Permissions);
        Assert.DoesNotContain(LisPermissions.ResultsApprove, payload.Permissions);
    }

    [Fact]
    public async Task Login_WithWrongPasswordOrBranch_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var wrongPassword = await client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest("MAIN", "manager.a", "Wrong@12345"));
        var wrongBranch = await client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest("OTHER", "manager.a", "Manager@12345"));

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongBranch.StatusCode);
    }

    [Fact]
    public async Task AdminUsers_IsDeniedWithoutTokenAndWithoutPermission()
    {
        var anonymousClient = _factory.CreateClient();
        var anonymousResponse = await anonymousClient.GetAsync("/admin/users");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        var receptionToken = await LoginAsync("MAIN", "reception.a", "Reception@12345");
        using var receptionClient = _factory.CreateClient();
        receptionClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", receptionToken);
        var deniedResponse = await receptionClient.GetAsync("/admin/users");
        Assert.Equal(HttpStatusCode.Forbidden, deniedResponse.StatusCode);
    }

    [Fact]
    public async Task AdminUsers_ReturnsOnlyUsersFromTheAuthenticatedBranch()
    {
        using var clientA = await CreateAuthenticatedClientAsync("MAIN", "manager.a", "Manager@12345");
        var responseA = await clientA.GetFromJsonAsync<List<UserSummary>>("/admin/users");
        Assert.NotNull(responseA);
        Assert.NotEmpty(responseA);
        Assert.All(responseA, user => Assert.Equal(_factory.BranchAId, user.BranchId));
        Assert.Contains(responseA, user => user.UserName == "manager.a");
        Assert.DoesNotContain(responseA, user => user.UserName == "manager.b");

        using var clientB = await CreateAuthenticatedClientAsync("OTHER", "manager.b", "Manager@12345");
        var responseB = await clientB.GetFromJsonAsync<List<UserSummary>>("/admin/users");
        Assert.NotNull(responseB);
        Assert.NotEmpty(responseB);
        Assert.All(responseB, user => Assert.Equal(_factory.BranchBId, user.BranchId));
        Assert.Contains(responseB, user => user.UserName == "manager.b");
        Assert.DoesNotContain(responseB, user => user.UserName == "manager.a");
    }

    [Fact]
    public async Task MultipleBranchScopedRoles_UnionTheirPermissions()
    {
        using var client = await CreateAuthenticatedClientAsync("MAIN", "multi.a", "Multi@12345");

        var me = await client.GetFromJsonAsync<MeResponse>("/auth/me");

        Assert.NotNull(me);
        Assert.Contains(LisPermissions.PatientsWrite, me.Permissions);
        Assert.Contains(LisPermissions.ResultsEnter, me.Permissions);
        Assert.DoesNotContain(LisPermissions.ResultsApprove, me.Permissions);
        Assert.DoesNotContain(LisPermissions.AdminUsers, me.Permissions);
    }

    [Fact]
    public async Task DeactivatedUser_CannotUseAnExistingToken()
    {
        var token = await LoginAsync("MAIN", "deactivatable.a", "Deactivate@12345");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LisDbContext>();
            var user = await db.Users.IgnoreQueryFilters()
                .SingleAsync(user => user.Id == _factory.DeactivatableUserId);
            user.IsActive = false;
            await db.SaveChangesAsync();
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RoleFromAnotherBranch_DoesNotGrantPermissions()
    {
        var token = await LoginAsync("MAIN", "crossbranch.a", "CrossBranch@12345");

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var me = await client.GetFromJsonAsync<MeResponse>("/auth/me");
        var adminResponse = await client.GetAsync("/admin/users");

        Assert.NotNull(me);
        Assert.Empty(me.Permissions);
        Assert.Equal(HttpStatusCode.Forbidden, adminResponse.StatusCode);
    }

    private async Task<string> LoginAsync(string branchCode, string userName, string password)
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(branchCode, userName, password));
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Login failed for {branchCode}/{userName}: {(int)response.StatusCode} {body}");
        }

        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(payload);
        return payload.Token;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(
        string branchCode,
        string userName,
        string password)
    {
        var token = await LoginAsync(branchCode, userName, password);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

/// <summary>
/// Shared in-memory database with one connection per request context, seeded
/// only with synthetic users and roles.
/// </summary>
public sealed class AuthTestFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _anchorConnection;
    private readonly string _connectionString;

    // Program reads JWT settings while the host is being built, before
    // WebApplicationFactory's in-memory configuration is applied. Set the
    // synthetic values as environment variables so issuance and validation
    // always use the same shared test key.
    static AuthTestFactory()
    {
        Environment.SetEnvironmentVariable("Jwt__Issuer", JwtTestConfiguration.Issuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", JwtTestConfiguration.Audience);
        Environment.SetEnvironmentVariable("Jwt__SigningKey", JwtTestConfiguration.SigningKey);
        Environment.SetEnvironmentVariable(
            "Jwt__ExpiryMinutes",
            JwtTestConfiguration.ExpiryMinutes.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    public AuthTestFactory()
    {
        var databaseName = $"auth-{Guid.NewGuid():N}";
        _connectionString = $"Data Source=file:{databaseName}?mode=memory&cache=shared";
        _anchorConnection = new SqliteConnection(_connectionString);
        _anchorConnection.Open();

        BranchAId = Guid.NewGuid();
        BranchBId = Guid.NewGuid();
        ManagerAUserId = Guid.NewGuid();
        ManagerBUserId = Guid.NewGuid();
        ReceptionAUserId = Guid.NewGuid();
        MultiAUserId = Guid.NewGuid();
        DeactivatableUserId = Guid.NewGuid();
        CrossBranchRoleUserId = Guid.NewGuid();

        SeedDatabaseAsync().GetAwaiter().GetResult();
    }

    public Guid BranchAId { get; }

    public Guid BranchBId { get; }

    public Guid ManagerAUserId { get; }

    public Guid ManagerBUserId { get; }

    public Guid ReceptionAUserId { get; }

    public Guid MultiAUserId { get; }

    public Guid DeactivatableUserId { get; }

    public Guid CrossBranchRoleUserId { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Lis"] = _connectionString,
                ["Jwt:Issuer"] = JwtTestConfiguration.Issuer,
                ["Jwt:Audience"] = JwtTestConfiguration.Audience,
                ["Jwt:SigningKey"] = JwtTestConfiguration.SigningKey,
                ["Jwt:ExpiryMinutes"] = JwtTestConfiguration.ExpiryMinutes
                    .ToString(System.Globalization.CultureInfo.InvariantCulture)
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<LisDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.AddDbContext<LisDbContext>(options =>
                options
                    .UseSqlite(_connectionString)
                    .UseBranchScope());
        });
    }

    private async Task SeedDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LisDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

        db.Database.Migrate();

        var branchA = new Branch { Id = BranchAId, Code = "MAIN", Name = "Synthetic Main" };
        var branchB = new Branch { Id = BranchBId, Code = "OTHER", Name = "Synthetic Other" };
        db.Branches.AddRange(branchA, branchB);

        var rolesA = CreateRoles(BranchAId);
        var rolesB = CreateRoles(BranchBId);
        db.Roles.AddRange(rolesA.Values);
        db.Roles.AddRange(rolesB.Values);

        var managerA = CreateUser(ManagerAUserId, BranchAId, "manager.a", "Manager A", passwordHasher, "Manager@12345");
        var managerB = CreateUser(ManagerBUserId, BranchBId, "manager.b", "Manager B", passwordHasher, "Manager@12345");
        var receptionA = CreateUser(ReceptionAUserId, BranchAId, "reception.a", "Reception A", passwordHasher, "Reception@12345");
        var multiA = CreateUser(MultiAUserId, BranchAId, "multi.a", "Multi Role A", passwordHasher, "Multi@12345");
        var deactivatable = CreateUser(DeactivatableUserId, BranchAId, "deactivatable.a", "Deactivatable A", passwordHasher, "Deactivate@12345");
        var crossBranch = CreateUser(CrossBranchRoleUserId, BranchAId, "crossbranch.a", "Cross Branch A", passwordHasher, "CrossBranch@12345");
        db.Users.AddRange(managerA, managerB, receptionA, multiA, deactivatable, crossBranch);

        db.UserRoles.AddRange(
            CreateUserRole(managerA, rolesA[LisRoles.Manager]),
            CreateUserRole(managerB, rolesB[LisRoles.Manager]),
            CreateUserRole(receptionA, rolesA[LisRoles.Reception]),
            CreateUserRole(multiA, rolesA[LisRoles.Reception]),
            CreateUserRole(multiA, rolesA[LisRoles.Technician]),
            CreateUserRole(deactivatable, rolesA[LisRoles.Reception]),
            CreateUserRole(crossBranch, rolesB[LisRoles.Manager]));

        await db.SaveChangesAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        _anchorConnection.Dispose();
    }

    private static Dictionary<string, Role> CreateRoles(Guid branchId)
    {
        return new[]
            {
                LisRoles.Reception,
                LisRoles.Technician,
                LisRoles.Reviewer,
                LisRoles.Accountant,
                LisRoles.Manager
            }
            .ToDictionary(
                name => name,
                name => new Role
                {
                    Id = Guid.NewGuid(),
                    BranchId = branchId,
                    Name = name,
                    Description = $"Synthetic {name} role"
                },
                StringComparer.OrdinalIgnoreCase);
    }

    private static User CreateUser(
        Guid id,
        Guid branchId,
        string userName,
        string fullName,
        IPasswordHasher<User> passwordHasher,
        string password)
    {
        var user = new User
        {
            Id = id,
            BranchId = branchId,
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            FullName = fullName,
            SecurityStamp = Guid.NewGuid().ToString("N")
        };
        user.PasswordHash = passwordHasher.HashPassword(user, password);
        return user;
    }

    private static UserRole CreateUserRole(User user, Role role) => new()
    {
        Id = Guid.NewGuid(),
        UserId = user.Id,
        RoleId = role.Id,
        BranchId = user.BranchId
    };
}
