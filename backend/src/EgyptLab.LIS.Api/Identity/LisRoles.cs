namespace EgyptLab.LIS.Api.Identity;

/// <summary>
/// Approved role names. A user may hold several branch-scoped roles; the
/// effective permission set is the union of their mapped permissions.
/// </summary>
public static class LisRoles
{
    public const string Reception = "Reception";
    public const string Technician = "Technician";
    public const string Reviewer = "Reviewer";
    public const string Accountant = "Accountant";
    public const string Manager = "Manager";

    public static IReadOnlyDictionary<string, IReadOnlyList<string>> PermissionsByRole { get; } =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [Reception] =
            [
                LisPermissions.PatientsRead,
                LisPermissions.PatientsWrite,
                LisPermissions.OrdersRead,
                LisPermissions.OrdersWrite,
                LisPermissions.InvoicesRead,
                LisPermissions.InvoicesWrite,
                LisPermissions.PaymentsRead,
                LisPermissions.PaymentsWrite
            ],
            [Technician] =
            [
                LisPermissions.CatalogueRead,
                LisPermissions.OrdersRead,
                LisPermissions.SamplesRead,
                LisPermissions.SamplesWrite,
                LisPermissions.ResultsRead,
                LisPermissions.ResultsEnter
            ],
            [Reviewer] =
            [
                LisPermissions.OrdersRead,
                LisPermissions.SamplesRead,
                LisPermissions.ResultsRead,
                LisPermissions.ResultsReview,
                LisPermissions.ResultsApprove,
                LisPermissions.ReportsRead,
                LisPermissions.ReportsExport
            ],
            [Accountant] =
            [
                LisPermissions.OrdersRead,
                LisPermissions.InvoicesRead,
                LisPermissions.InvoicesWrite,
                LisPermissions.PaymentsRead,
                LisPermissions.PaymentsWrite,
                LisPermissions.FinanceReports
            ],
            [Manager] =
            [
                LisPermissions.PatientsRead,
                LisPermissions.OrdersRead,
                LisPermissions.CatalogueRead,
                LisPermissions.SamplesRead,
                LisPermissions.ResultsRead,
                LisPermissions.InvoicesRead,
                LisPermissions.PaymentsRead,
                LisPermissions.ReportsRead,
                LisPermissions.ReportsExport,
                LisPermissions.FinanceReports,
                LisPermissions.AdminUsers,
                LisPermissions.AdminRoles,
                LisPermissions.AdminBranches
            ]
        };

    /// <summary>
    /// Unknown role names grant nothing. This fail-closed behavior prevents a
    /// renamed or stale role from silently gaining access.
    /// </summary>
    public static IReadOnlyList<string> ResolvePermissions(IEnumerable<string> roleNames)
    {
        var permissions = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var roleName in roleNames)
        {
            if (PermissionsByRole.TryGetValue(roleName, out var mapped))
            {
                permissions.UnionWith(mapped);
            }
        }

        return permissions.ToList();
    }
}
