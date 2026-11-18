namespace EgyptLab.LIS.Infrastructure.Persistence;

/// <summary>
/// Identity of the authenticated user performing the current operation. The
/// API middleware sets it only after token validation and clears it when the
/// request ends, so audit rows cannot use a caller-supplied identity.
/// </summary>
public static class AuditActor
{
    private static readonly AsyncLocal<AuditIdentity?> Current = new();

    public static Guid? UserId => Current.Value?.UserId;

    public static Guid? BranchId => Current.Value?.BranchId;

    public static string? Role => Current.Value?.Role;

    public static string? IpAddress => Current.Value?.IpAddress;

    public static void Set(Guid userId, Guid branchId, string? role, string? ipAddress)
    {
        Current.Value = new AuditIdentity(userId, branchId, role, ipAddress);
    }

    public static void Clear()
    {
        Current.Value = null;
    }

    private sealed record AuditIdentity(
        Guid UserId,
        Guid BranchId,
        string? Role,
        string? IpAddress);
}
