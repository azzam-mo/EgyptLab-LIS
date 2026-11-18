namespace EgyptLab.LIS.Api.Identity;

/// <summary>
/// Permission names are stable strings stored in tokens and authorization
/// policies. They are deliberately separate from UI text.
/// </summary>
public static class LisPermissions
{
    public const string PatientsRead = "patients.read";
    public const string PatientsWrite = "patients.write";
    public const string OrdersRead = "orders.read";
    public const string OrdersWrite = "orders.write";
    public const string CatalogueRead = "catalogue.read";
    public const string CatalogueWrite = "catalogue.write";
    public const string SamplesRead = "samples.read";
    public const string SamplesWrite = "samples.write";
    public const string ResultsRead = "results.read";
    public const string ResultsEnter = "results.enter";
    public const string ResultsReview = "results.review";
    public const string ResultsApprove = "results.approve";
    public const string ReportsRead = "reports.read";
    public const string ReportsExport = "reports.export";
    public const string InvoicesRead = "invoices.read";
    public const string InvoicesWrite = "invoices.write";
    public const string PaymentsRead = "payments.read";
    public const string PaymentsWrite = "payments.write";
    public const string FinanceReports = "finance.reports";
    public const string AdminUsers = "admin.users";
    public const string AdminRoles = "admin.roles";
    public const string AdminBranches = "admin.branches";

    public static IReadOnlyList<string> All { get; } =
    [
        PatientsRead,
        PatientsWrite,
        OrdersRead,
        OrdersWrite,
        CatalogueRead,
        CatalogueWrite,
        SamplesRead,
        SamplesWrite,
        ResultsRead,
        ResultsEnter,
        ResultsReview,
        ResultsApprove,
        ReportsRead,
        ReportsExport,
        InvoicesRead,
        InvoicesWrite,
        PaymentsRead,
        PaymentsWrite,
        FinanceReports,
        AdminUsers,
        AdminRoles,
        AdminBranches
    ];
}
