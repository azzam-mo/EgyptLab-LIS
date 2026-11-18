using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EgyptLab.LIS.Infrastructure.Persistence;

public sealed record DatabaseMigrationReport(
    IReadOnlyList<string> AppliedMigrations,
    string JournalMode,
    IReadOnlyList<string> AuditTriggers);

/// <summary>
/// Explicit database maintenance used by the deployment command. Migrations
/// are never applied automatically at application startup: an operator must
/// run <c>--migrate --confirm</c> after taking a backup.
/// </summary>
public static class DatabaseMaintenance
{
    public const string WalMode = "wal";

    private static readonly string[] RequiredAuditTriggers =
    [
        "audit_log_no_update",
        "audit_log_no_delete"
    ];

    public static async Task<IReadOnlyList<string>> GetPendingMigrationsAsync(
        LisDbContext db,
        CancellationToken cancellationToken = default)
    {
        var pending = await db.Database.GetPendingMigrationsAsync(cancellationToken);
        return pending.Order(StringComparer.Ordinal).ToList();
    }

    public static async Task<DatabaseMigrationReport> ApplyMigrationsAsync(
        LisDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var pending = await GetPendingMigrationsAsync(db, cancellationToken);

        if (pending.Count > 0)
        {
            logger.LogInformation(
                "Applying {Count} pending migration(s): {Migrations}",
                pending.Count,
                string.Join(", ", pending));
            await db.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            logger.LogInformation("No pending migrations.");
        }

        var journalMode = await EnsureWalModeAsync(db, logger, cancellationToken);
        var triggers = await VerifyAuditTriggersAsync(db, cancellationToken);
        await VerifyIntegrityAsync(db, cancellationToken);

        return new DatabaseMigrationReport(pending, journalMode, triggers);
    }

    public static async Task<string> EnsureWalModeAsync(
        LisDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA journal_mode = WAL;";
            var result = Convert.ToString(
                await command.ExecuteScalarAsync(cancellationToken),
                System.Globalization.CultureInfo.InvariantCulture);

            if (!string.Equals(result, WalMode, StringComparison.OrdinalIgnoreCase))
            {
                // In-memory test databases cannot use WAL. A file database
                // failing to enter WAL is a deployment problem, not a warning.
                if (connection.DataSource.Contains(":memory:", StringComparison.Ordinal)
                    || connection.DataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogWarning(
                        "SQLite journal mode is {JournalMode}; WAL is not available for this database.",
                        result);
                }
                else
                {
                    throw new InvalidOperationException(
                        $"SQLite journal mode is '{result}', expected '{WalMode}'.");
                }
            }

            return result ?? "unknown";
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    public static async Task<IReadOnlyList<string>> VerifyAuditTriggersAsync(
        LisDbContext db,
        CancellationToken cancellationToken = default)
    {
        var triggers = await db.Database
            .SqlQueryRaw<string>(
                """
                SELECT name AS "Value"
                FROM sqlite_master
                WHERE type = 'trigger'
                  AND name IN ('audit_log_no_update', 'audit_log_no_delete')
                ORDER BY name
                """)
            .ToListAsync(cancellationToken);

        var missing = RequiredAuditTriggers
            .Where(required => !triggers.Contains(required, StringComparer.Ordinal))
            .ToList();

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Required append-only audit triggers are missing: {string.Join(", ", missing)}.");
        }

        return triggers;
    }

    public static async Task VerifyIntegrityAsync(
        LisDbContext db,
        CancellationToken cancellationToken = default)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using (var quickCheck = connection.CreateCommand())
            {
                quickCheck.CommandText = "PRAGMA quick_check;";
                var result = Convert.ToString(
                    await quickCheck.ExecuteScalarAsync(cancellationToken),
                    System.Globalization.CultureInfo.InvariantCulture);

                if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"SQLite quick_check did not return ok: {result}");
                }
            }

            await using var foreignKeyCheck = connection.CreateCommand();
            foreignKeyCheck.CommandText = "PRAGMA foreign_key_check;";
            await using var reader = await foreignKeyCheck.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException(
                    "SQLite foreign_key_check reported at least one violation.");
            }
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }
}
