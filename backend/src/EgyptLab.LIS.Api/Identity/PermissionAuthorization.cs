using Microsoft.AspNetCore.Authorization;

namespace EgyptLab.LIS.Api.Identity;

/// <summary>
/// Requires one exact permission and a resolvable branch claim.
/// </summary>
public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

public sealed class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        var branchClaim = context.User.FindFirst(LisClaims.BranchId)?.Value;
        if (!Guid.TryParse(branchClaim, out _))
        {
            return Task.CompletedTask;
        }

        if (context.User.HasClaim(LisClaims.Permission, requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
