namespace EgyptLab.LIS.Infrastructure.Persistence;

/// <summary>
/// Branch identity of the current request. It is set after authentication and
/// cleared at the end of the request so an unscoped query fails closed.
/// </summary>
public interface IBranchScope
{
    Guid? BranchId { get; }

    void Set(Guid branchId);

    void Clear();
}

/// <summary>
/// AsyncLocal storage avoids EF Core model-cache leakage between requests while
/// still allowing query filters to read the value at query execution time.
/// </summary>
public sealed class BranchScope : IBranchScope
{
    private static readonly AsyncLocal<Guid?> CurrentBranch = new();

    /// <summary>
    /// Used by EF query filters. A null value makes every branch-scoped query
    /// return no rows.
    /// </summary>
    public static Guid? CurrentBranchId => CurrentBranch.Value;

    public Guid? BranchId => CurrentBranch.Value;

    public void Set(Guid branchId) => CurrentBranch.Value = branchId;

    public void Clear() => CurrentBranch.Value = null;
}
