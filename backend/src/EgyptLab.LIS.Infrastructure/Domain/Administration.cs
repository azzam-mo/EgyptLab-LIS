namespace EgyptLab.LIS.Infrastructure.Domain;

/// <summary>
/// A laboratory site. For this table the branch scope is the branch itself.
/// </summary>
public sealed class Branch : AuditedEntity
{
    public Branch()
    {
        BranchId = Id;
    }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// A named permission set. Role scope semantics are approved in Phase 0.4.
/// </summary>
public sealed class Role : AuditedEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}

/// <summary>
/// A staff account. Authentication fields are added with ASP.NET Core Identity.
/// </summary>
public sealed class User : AuditedEntity
{
    public string UserName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Explicit join between users and roles. Kept as a table so it carries the
/// same UUID, branch and soft-delete columns as every other table.
/// </summary>
public sealed class UserRole : AuditedEntity
{
    public Guid UserId { get; set; }

    public Guid RoleId { get; set; }

    public User User { get; set; } = null!;

    public Role Role { get; set; } = null!;
}
