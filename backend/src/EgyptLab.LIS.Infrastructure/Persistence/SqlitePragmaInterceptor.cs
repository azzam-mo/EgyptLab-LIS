using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EgyptLab.LIS.Infrastructure.Persistence;

/// <summary>
/// Applies the per-connection SQLite settings that keep concurrent writers
/// predictable. WAL mode itself is persisted in the database file by the
/// explicit migration command; these pragmas are connection-local.
/// </summary>
public sealed class SqlitePragmaInterceptor : DbConnectionInterceptor
{
    public static SqlitePragmaInterceptor Instance { get; } = new();

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        Apply(connection);
        base.ConnectionOpened(connection, eventData);
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        Apply(connection);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    private static void Apply(DbConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            PRAGMA foreign_keys = ON;
            PRAGMA busy_timeout = 5000;
            PRAGMA synchronous = NORMAL;
            """;
        command.ExecuteNonQuery();
    }
}
