namespace EgyptLab.LIS.Infrastructure.Persistence;

/// <summary>
/// EF Core 10 requires either one anonymous filter or only named filters.
/// Both global rules are named so they can coexist and be tested separately.
/// </summary>
internal static class QueryFilterKeys
{
    public const string SoftDelete = "SoftDelete";
    public const string BranchScope = "BranchScope";
}
