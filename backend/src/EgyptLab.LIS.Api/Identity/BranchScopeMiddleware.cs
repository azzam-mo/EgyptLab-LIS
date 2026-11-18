using EgyptLab.LIS.Infrastructure.Persistence;

namespace EgyptLab.LIS.Api.Identity;

/// <summary>
/// Applies the authenticated branch to Entity Framework query filters. It runs
/// after token authentication and always clears the value before the request
/// ends.
/// </summary>
public sealed class BranchScopeMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IBranchScope branchScope)
    {
        var branchClaim = context.User.FindFirst(LisClaims.BranchId)?.Value;
        if (Guid.TryParse(branchClaim, out var branchId))
        {
            branchScope.Set(branchId);
        }

        try
        {
            await next(context);
        }
        finally
        {
            branchScope.Clear();
        }
    }
}
