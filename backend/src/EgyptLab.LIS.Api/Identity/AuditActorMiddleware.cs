using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EgyptLab.LIS.Infrastructure.Persistence;

namespace EgyptLab.LIS.Api.Identity;

/// <summary>
/// Makes the validated authenticated identity available to the audit writer.
/// Token validation has already checked the active user, branch, and security
/// stamp. Anonymous requests leave the actor unset, so login updates cannot
/// create misleading actor records.
/// </summary>
public sealed class AuditActorMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Never inherit an actor from a previous request or a parent flow.
        AuditActor.Clear();

        if (context.User.Identity?.IsAuthenticated == true)
        {
            var subject = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var branch = context.User.FindFirst(LisClaims.BranchId)?.Value;

            if (Guid.TryParse(subject, out var userId)
                && Guid.TryParse(branch, out var branchId))
            {
                var role = context.User.FindFirst(ClaimTypes.Role)?.Value
                    ?? context.User.FindFirst("role")?.Value;
                var ipAddress = context.Connection.RemoteIpAddress?.ToString();
                AuditActor.Set(userId, branchId, role, ipAddress);
            }
        }

        try
        {
            await next(context);
        }
        finally
        {
            AuditActor.Clear();
        }
    }
}
