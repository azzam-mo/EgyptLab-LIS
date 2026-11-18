using System.Runtime.CompilerServices;
using System.Text.Json;
using EgyptLab.LIS.Infrastructure.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EgyptLab.LIS.Infrastructure.Persistence;

/// <summary>
/// Writes one append-only audit row for every approved clinical or financial
/// entity change in the same SaveChanges transaction. The interceptor is added
/// by <see cref="LisDbContext.OnConfiguring"/> so every context, including
/// tests and design-time contexts, uses the same write path.
/// </summary>
internal sealed class AuditWriter : SaveChangesInterceptor
{
    private const string Redacted = "[redacted]";

    // Authentication secrets and credential-like properties are never copied
    // into audit snapshots, even if a future audited entity exposes one.
    private static readonly HashSet<string> AlwaysRedactedProperties =
        new(StringComparer.OrdinalIgnoreCase)
        {
            nameof(User.PasswordHash),
            nameof(User.SecurityStamp),
            "Password",
            "Token",
            "AccessToken",
            "RefreshToken",
            "Secret",
            "ApiKey",
            "PrivateKey",
            "ConnectionString"
        };

    // Direct patient identifiers are redacted in audit JSON. The audit row
    // still proves which fields changed and carries the clinical/financial
    // values needed for traceability; audit_log access is protected separately
    // by branch scope and later read permissions.
    private static readonly HashSet<string> PatientDirectIdentifiers =
        new(StringComparer.OrdinalIgnoreCase)
        {
            nameof(Patient.FirstName),
            nameof(Patient.LastName),
            nameof(Patient.NationalId),
            nameof(Patient.Phone),
            nameof(Patient.DateOfBirth)
        };

    private readonly ConditionalWeakTable<DbContext, List<AuditLog>> _pendingAuditRows = new();

    public static AuditWriter Instance { get; } = new();

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            RecordChanges(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            RecordChanges(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        DetachPendingRows(eventData.Context);
        base.SaveChangesFailed(eventData);
    }

    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        DetachPendingRows(eventData.Context);
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        ClearPendingRows(eventData.Context);
        return base.SavedChanges(eventData, result);
    }

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        ClearPendingRows(eventData.Context);
        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private void RecordChanges(DbContext context)
    {
        if (context is not LisDbContext lisContext)
        {
            return;
        }

        context.ChangeTracker.DetectChanges();

        var pending = GetOrCreatePending(context);
        var pendingRows = pending.ToHashSet();
        var auditEntries = context.ChangeTracker.Entries<AuditLog>().ToList();
        var forgedAuditRows = auditEntries.Any(entry =>
            entry.State is EntityState.Modified or EntityState.Deleted
            || (entry.State == EntityState.Added && !pendingRows.Contains((AuditLog)entry.Entity)));

        if (forgedAuditRows)
        {
            throw new InvalidOperationException(
                "audit_log is append-only; updates, deletes, and direct inserts are not allowed.");
        }

        // Only tracked SaveChanges operations are audited. ExecuteUpdate,
        // ExecuteDelete, and raw SQL against these tables bypass this
        // interceptor and must not be used for audited entities. See ADR-0004.
        var entities = context.ChangeTracker.Entries()
            .Where(entry => entry.Entity is Patient or Result or Invoice)
            .ToList();

        foreach (var entry in entities.Where(entry => entry.State == EntityState.Deleted))
        {
            throw new InvalidOperationException(
                $"{entry.Metadata.ClrType.Name} rows cannot be hard-deleted; use soft delete.");
        }

        var currentBranch = BranchScope.CurrentBranchId;
        var actorBranch = AuditActor.BranchId;
        var changes = new List<AuditChange>();

        // Validate every entity before adding any audit row so a rejected
        // change cannot leave partially generated audit rows in the tracker.
        foreach (var entry in entities)
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            if (entry.State == EntityState.Modified
                && entry.Property(nameof(AuditedEntity.BranchId)).IsModified)
            {
                throw new InvalidOperationException(
                    "The branch of an audited entity cannot be changed.");
            }

            var entity = (AuditedEntity)entry.Entity;
            if (entity.BranchId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    $"Audited entity {entry.Metadata.ClrType.Name} must have a branch.");
            }

            if (currentBranch is not null && entity.BranchId != currentBranch)
            {
                throw new InvalidOperationException(
                    "The audited entity branch does not match the current branch scope.");
            }

            if (actorBranch is not null && entity.BranchId != actorBranch)
            {
                throw new InvalidOperationException(
                    "The audited entity branch does not match the authenticated actor branch.");
            }

            if (entry.State == EntityState.Added)
            {
                changes.Add(new AuditChange(entry, entity, "create", null, Snapshot(entry)));
                continue;
            }

            var (action, before, after) = DescribeUpdate(entry);
            if (action is not null)
            {
                changes.Add(new AuditChange(entry, entity, action, before, after));
            }
        }

        var auditRows = changes
            .Select(change => new AuditLog
            {
                Id = Guid.NewGuid(),
                BranchId = change.Entity.BranchId,
                EntityName = change.Entry.Metadata.ClrType.Name,
                EntityId = change.Entity.Id,
                Action = change.Action,
                ActorUserId = AuditActor.UserId,
                ActorRole = AuditActor.Role,
                IpAddress = AuditActor.IpAddress,
                OccurredAt = DateTimeOffset.UtcNow,
                BeforeJson = change.Before is null ? null : JsonSerializer.Serialize(change.Before),
                AfterJson = change.After is null ? null : JsonSerializer.Serialize(change.After)
            })
            .ToList();

        foreach (var audit in auditRows)
        {
            lisContext.AuditLog.Add(audit);
            pending.Add(audit);
        }
    }

    private sealed record AuditChange(
        EntityEntry Entry,
        AuditedEntity Entity,
        string Action,
        Dictionary<string, object?>? Before,
        Dictionary<string, object?>? After);

    private static (string? Action, Dictionary<string, object?>? Before, Dictionary<string, object?>? After)
        DescribeUpdate(EntityEntry entry)
    {
        var modifiedProperties = entry.Properties
            .Where(property => property.IsModified)
            .ToList();

        if (modifiedProperties.Count == 0)
        {
            return (null, null, null);
        }

        var before = new Dictionary<string, object?>(StringComparer.Ordinal);
        var after = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var property in modifiedProperties)
        {
            var name = property.Metadata.Name;
            before[name] = Redact(entry.Entity, name, property.OriginalValue);
            after[name] = Redact(entry.Entity, name, property.CurrentValue);
        }

        var deletedAt = entry.Property(nameof(AuditedEntity.DeletedAt));
        var isSoftDelete = deletedAt.IsModified && deletedAt.CurrentValue is DateTimeOffset;

        return (isSoftDelete ? "delete" : "update", before, after);
    }

    private static Dictionary<string, object?> Snapshot(EntityEntry entry)
    {
        var snapshot = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var property in entry.Properties)
        {
            var name = property.Metadata.Name;
            snapshot[name] = Redact(entry.Entity, name, property.CurrentValue);
        }

        return snapshot;
    }

    private static object? Redact(object entity, string propertyName, object? value)
    {
        if (AlwaysRedactedProperties.Contains(propertyName))
        {
            return Redacted;
        }

        if (entity is Patient && PatientDirectIdentifiers.Contains(propertyName))
        {
            return Redacted;
        }

        return value;
    }

    private List<AuditLog> GetOrCreatePending(DbContext context) =>
        _pendingAuditRows.GetValue(context, _ => []);

    private void DetachPendingRows(DbContext? context)
    {
        if (context is null || !_pendingAuditRows.TryGetValue(context, out var rows))
        {
            return;
        }

        foreach (var row in rows)
        {
            var entry = context.Entry(row);
            if (entry.State == EntityState.Added)
            {
                entry.State = EntityState.Detached;
            }
        }

        _pendingAuditRows.Remove(context);
    }

    private void ClearPendingRows(DbContext? context)
    {
        if (context is not null)
        {
            _pendingAuditRows.Remove(context);
        }
    }
}
