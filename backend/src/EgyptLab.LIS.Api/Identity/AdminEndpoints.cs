using EgyptLab.LIS.Infrastructure.Domain;
using EgyptLab.LIS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EgyptLab.LIS.Api.Identity;

public sealed record UserSummary(
    Guid Id,
    string UserName,
    string FullName,
    Guid BranchId,
    IReadOnlyList<string> Roles);

public sealed record RoleSummary(Guid Id, string Name, string? Description, Guid BranchId);

public static class AdminEndpoints
{
    public static RouteGroupBuilder MapAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/admin").WithTags("Administration");

        group.MapGet("/users", async (LisDbContext db, CancellationToken cancellationToken) =>
        {
            // The branch query filter from LisDbContext is the only scope used
            // here; no handler-supplied branch parameter can be bypassed.
            var users = await db.Users
                .OrderBy(user => user.UserName)
                .Select(user => new
                {
                    user.Id,
                    user.UserName,
                    user.FullName,
                    user.BranchId
                })
                .ToListAsync(cancellationToken);

            var rolesByUser = await db.UserRoles
                .Where(link => link.DeletedAt == null)
                .Select(link => new { link.UserId, RoleName = link.Role.Name })
                .ToListAsync(cancellationToken);

            var result = users.Select(user => new UserSummary(
                    user.Id,
                    user.UserName,
                    user.FullName,
                    user.BranchId,
                    rolesByUser
                        .Where(link => link.UserId == user.Id)
                        .Select(link => link.RoleName)
                        .Order()
                        .ToList()))
                .ToList();

            return Results.Ok(result);
        }).RequireAuthorization(LisPermissions.AdminUsers);

        group.MapGet("/roles", async (LisDbContext db, CancellationToken cancellationToken) =>
        {
            var roles = await db.Roles
                .OrderBy(role => role.Name)
                .Select(role => new RoleSummary(role.Id, role.Name, role.Description, role.BranchId))
                .ToListAsync(cancellationToken);

            return Results.Ok(roles);
        }).RequireAuthorization(LisPermissions.AdminRoles);

        group.MapPost("/users/{userId:guid}/roles/{roleId:guid}", async (
            Guid userId,
            Guid roleId,
            LisDbContext db,
            CancellationToken cancellationToken) =>
        {
            var user = await db.Users.SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
            var role = await db.Roles.SingleOrDefaultAsync(item => item.Id == roleId, cancellationToken);

            if (user is null || role is null)
            {
                return Results.NotFound();
            }

            if (role.BranchId != user.BranchId)
            {
                return Results.BadRequest(new { error = "A role can only be assigned inside its own branch." });
            }

            var alreadyAssigned = await db.UserRoles
                .AnyAsync(link => link.UserId == userId && link.RoleId == roleId, cancellationToken);

            if (alreadyAssigned)
            {
                return Results.Conflict();
            }

            var link = new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                BranchId = user.BranchId
            };

            db.UserRoles.Add(link);
            await db.SaveChangesAsync(cancellationToken);

            return Results.Created($"/admin/users/{userId}/roles/{roleId}", new { link.Id, userId, roleId });
        }).RequireAuthorization(LisPermissions.AdminUsers);

        return group;
    }
}
