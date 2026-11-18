using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using EgyptLab.LIS.Api.Identity;
using EgyptLab.LIS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace EgyptLab.LIS.Api.Tests;

/// <summary>
/// Task 0.4.1: prove that bearer tokens are rejected for every relevant
/// validation failure and that a correctly signed HS256 token still works.
/// All identities and keys are synthetic.
/// </summary>
public sealed class JwtNegativeIntegrationTests : IClassFixture<AuthTestFactory>
{
    private const string ForgedSigningKey = "forged-signing-key-used-only-by-an-attacker";

    private readonly AuthTestFactory _factory;

    public JwtNegativeIntegrationTests(AuthTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ForgedSignature_IsRejected()
    {
        var token = CreateSignedToken(ValidClaims(), signingKey: ForgedSigningKey);

        await AssertUnauthorizedAsync(token);
    }

    [Fact]
    public async Task WrongIssuer_IsRejected()
    {
        var token = CreateSignedToken(ValidClaims(), issuer: "https://untrusted-issuer.example");

        await AssertUnauthorizedAsync(token);
    }

    [Fact]
    public async Task WrongAudience_IsRejected()
    {
        var token = CreateSignedToken(ValidClaims(), audience: "untrusted-client");

        await AssertUnauthorizedAsync(token);
    }

    [Fact]
    public async Task ExpiredToken_IsRejected()
    {
        var now = DateTimeOffset.UtcNow;
        var token = CreateSignedToken(
            ValidClaims(),
            notBefore: now.AddMinutes(-20),
            expires: now.AddMinutes(-10));

        await AssertUnauthorizedAsync(token);
    }

    [Fact]
    public async Task FutureNotBefore_IsRejected()
    {
        var now = DateTimeOffset.UtcNow;
        var token = CreateSignedToken(
            ValidClaims(),
            notBefore: now.AddMinutes(10),
            expires: now.AddMinutes(30));

        await AssertUnauthorizedAsync(token);
    }

    [Fact]
    public async Task MalformedToken_IsRejected()
    {
        await AssertUnauthorizedAsync("not-a-jwt");
    }

    [Fact]
    public async Task MissingBranchClaim_IsRejected()
    {
        var claims = ValidClaims()
            .Where(claim => claim.Type != LisClaims.BranchId)
            .ToList();
        var token = CreateSignedToken(claims);

        await AssertUnauthorizedAsync(token);
    }

    [Fact]
    public async Task MissingSecurityStamp_IsRejected()
    {
        var claims = ValidClaims()
            .Where(claim => claim.Type != LisClaims.SecurityStamp)
            .ToList();
        var token = CreateSignedToken(claims);

        await AssertUnauthorizedAsync(token);
    }

    [Fact]
    public async Task UnexpectedHs384Algorithm_IsRejected()
    {
        var token = CreateSignedToken(ValidClaims(), signingAlgorithm: SecurityAlgorithms.HmacSha384);

        await AssertUnauthorizedAsync(token);
    }

    [Fact]
    public async Task ValidHs256Token_IsAccepted()
    {
        var token = CreateSignedToken(ValidClaims());

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await response.Content.ReadFromJsonAsync<MeResponse>();
        Assert.NotNull(me);
        Assert.Equal(_factory.ManagerAUserId, me.UserId);
        Assert.Equal(_factory.BranchAId, me.BranchId);
    }

    private async Task AssertUnauthorizedAsync(string token)
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private List<Claim> ValidClaims()
    {
        var identity = ReadManagerIdentity();

        return
        [
            new Claim(JwtRegisteredClaimNames.Sub, identity.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, "MANAGER.A"),
            new Claim(LisClaims.DisplayName, "Manager A"),
            new Claim(LisClaims.BranchId, identity.BranchId.ToString()),
            new Claim(LisClaims.SecurityStamp, identity.SecurityStamp)
        ];
    }

    private (Guid UserId, Guid BranchId, string SecurityStamp) ReadManagerIdentity()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LisDbContext>();
        var user = db.Users.IgnoreQueryFilters()
            .AsNoTracking()
            .Single(item => item.Id == _factory.ManagerAUserId);

        return (user.Id, user.BranchId, user.SecurityStamp);
    }

    private string CreateSignedToken(
        IEnumerable<Claim> claims,
        string? issuer = null,
        string? audience = null,
        string? signingKey = null,
        string signingAlgorithm = SecurityAlgorithms.HmacSha256,
        DateTimeOffset? notBefore = null,
        DateTimeOffset? expires = null)
    {
        var now = DateTimeOffset.UtcNow;
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(signingKey ?? JwtTestConfiguration.SigningKey));
        var credentials = new SigningCredentials(key, signingAlgorithm);

        var token = new JwtSecurityToken(
            issuer: issuer ?? JwtTestConfiguration.Issuer,
            audience: audience ?? JwtTestConfiguration.Audience,
            claims: claims,
            notBefore: (notBefore ?? now.AddMinutes(-1)).UtcDateTime,
            expires: (expires ?? now.AddMinutes(JwtTestConfiguration.ExpiryMinutes)).UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
