using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EgyptLab.LIS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AuditLogAppendOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The EF interceptor rejects audit_log updates and deletes during
            // normal application writes. These SQLite triggers also reject
            // accidental or malicious SQL that bypasses the interceptor.
            // A PostgreSQL equivalent is required when synchronization adds
            // that provider; see ADR-0004.
            migrationBuilder.Sql(
                """
                CREATE TRIGGER audit_log_no_update
                BEFORE UPDATE ON audit_log
                BEGIN
                    SELECT RAISE(ABORT, 'audit_log is append-only: updates are not allowed');
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER audit_log_no_delete
                BEFORE DELETE ON audit_log
                BEGIN
                    SELECT RAISE(ABORT, 'audit_log is append-only: deletes are not allowed');
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS audit_log_no_update;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS audit_log_no_delete;");
        }
    }
}
