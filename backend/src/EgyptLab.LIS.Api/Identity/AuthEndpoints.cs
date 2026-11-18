using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EgyptLab.LIS.Infrastructure.Domain;
using EgyptLab.LIS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EgyptLab.LIS.Api.Identity;

public sealed record LoginRequest(string BranchCode, string UserName, string Password);

public sealed record LoginResponse(
    string Token,
    DateTimeOffset ExpiresAt,
    Guid BranchId,
    string UserName,
    IReadOnlyList<string> Permissions);

public sealed record MeResponse(
    Guid UserId,
    Guid BranchId,
    string UserName,
    string FullName,
    IReadOnlyList<string> Permissions);

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/auth").WithTags("Authentication");

        group.MapPost("/login", async (
            LoginRequest request,
            LisDbContext db,
            IPasswordHasher<User> passwordHasher,
            JwtTokenService tokenService,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.BranchCode)
                || string.IsNullOrWhiteSpace(request.UserName)
                || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new { error = "BranchCode, UserName and Password are required." });
            }

            var branchCode = request.BranchCode.Trim().ToUpperInvariant();
            var normalizedUserName = request.UserName.Trim().ToUpperInvariant();

            // Login has no branch scope yet, so the global filters must be
            // bypassed and branch membership checked explicitly.
            var branch = await db.Branches.IgnoreQueryFilters()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.Code == branchCode && item.IsActive && item.DeletedAt == null,
                    cancellationToken);

            if (branch is null)
            {
                return Results.Unauthorized();
            }

            var user = await db.Users.IgnoreQueryFilters()
                .SingleOrDefaultAsync(
                    item => item.BranchId == branch.Id
                        && item.NormalizedUserName == normalizedUserName
                        && item.IsActive
                        && item.DeletedAt == null,
                    cancellationToken);

            if (user is null)
            {
                return Results.Unauthorized();
            }

            var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (verification == PasswordVerificationResult.Failed)
            {
                return Results.Unauthorized();
            }

            if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
            }

            var roleNames = await db.UserRoles.IgnoreQueryFilters()
                .Where(link => link.UserId == user.Id
                    && link.DeletedAt == null
                    && link.Role.BranchId == user.BranchId
                    && link.Role.DeletedAt == null)
                .Select(link => link.Role.Name)
                .ToListAsync(cancellationToken);

            var permissions = LisRoles.ResolvePermissions(roleNames);
            var issued = tokenService.Create(user, permissions);
            user.LastLoginAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok(new LoginResponse(
                issued.Value,
                issued.ExpiresAt,
                user.BranchId,
                user.UserName,
                permissions));
        }).AllowAnonymous();

        group.MapGet("/me", async (
            ClaimsPrincipal principal,
            LisDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (!TryReadIdentity(principal, out var userId, out var branchId, out var securityStamp))
            {
                return Results.Unauthorized();
            }

            var user = await db.Users.IgnoreQueryFilters()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.Id == userId
                        && item.BranchId == branchId
                        && item.IsActive
                        && item.DeletedAt == null
                        && item.SecurityStamp == securityStamp,
                    cancellationToken);

            if (user is null)
            {
                return Results.Unauthorized();
            }

            var permissions = principal.FindAll(LisClaims.Permission)
                .Select(claim => claim.Value)
                .Distinct(StringComparer.Ordinal)
                .Order()
                .ToList();

            return Results.Ok(new MeResponse(
                user.Id,
                user.BranchId,
                user.UserName,
                user.FullName,
                permissions));
        }).RequireAuthorization();

        return group;
    }

    internal static bool TryReadIdentity(
        ClaimsPrincipal principal,
        out Guid userId,
        out Guid branchId,
        out string securityStamp)
    {
        userId = Guid.Empty;
        branchId = Guid.Empty;
        securityStamp = string.Empty;

        var subject = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var branch = principal.FindFirst(LisClaims.BranchId)?.Value;
        var stamp = principal.FindFirst(LisClaims.SecurityStamp)?.Value;

        if (Guid.TryParse(subject, out userId)
            && Guid.TryParse(branch, out branchId)
            && !string.IsNullOrWhiteSpace(stamp))
        {
            securityStamp = stamp;
            return true;
        }

        return false;
    }

    internal static bool StampMatches(string expected, string actual)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var actualBytes = Encoding.UTF8.GetBytes(actual);
        return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }
}
