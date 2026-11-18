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

    public override Guid Id
    {
        get => base.Id;
        set
        {
            base.Id = value;
            BranchId = value;
        }
    }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// A branch-scoped named permission set. Approved role-permission mapping is
/// defined in the API authorization layer.
/// </summary>
public sealed class Role : AuditedEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}

/// <summary>
/// A staff account. Passwords are stored only as Identity password hashes;
/// no plaintext password or recovery secret is persisted.
/// </summary>
public sealed class User : AuditedEntity
{
    public string UserName { get; set; } = string.Empty;

    public string NormalizedUserName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Changes invalidate existing tokens. This gives revocation without a
    /// separate session table in the local-first database.
    /// </summary>
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");

    public bool IsActive { get; set; } = true;

    public DateTimeOffset? LastLoginAt { get; set; }
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
