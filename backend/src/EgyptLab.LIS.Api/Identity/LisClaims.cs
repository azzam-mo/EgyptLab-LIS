namespace EgyptLab.LIS.Api.Identity;

/// <summary>
/// Claim types carried in the signed access token.
/// </summary>
public static class LisClaims
{
    public const string BranchId = "branch_id";
    public const string Permission = "permission";
    public const string SecurityStamp = "security_stamp";
    public const string DisplayName = "display_name";
}
