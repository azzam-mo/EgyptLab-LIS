using EgyptLab.LIS.Infrastructure.Domain;
using EgyptLab.LIS.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace EgyptLab.LIS.Infrastructure.Tests;

/// <summary>
/// These tests guard the schema rules that protect patients and money. They
/// inspect the EF model and apply the real migration to an empty SQLite file.
/// </summary>
public sealed class SchemaInvariantTests
{
    private const int ExpectedTableCount = 18;

    private static LisDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<LisDbContext>()
            .UseSqlite(connection)
            .Options;

        return new LisDbContext(options);
    }

    private static List<IEntityType> DomainEntityTypes(LisDbContext context)
    {
        return context.Model.GetEntityTypes()
            .Where(type => type.ClrType?.Namespace?.StartsWith(
                "EgyptLab.LIS.Infrastructure.Domain", StringComparison.Ordinal) == true)
            .ToList();
    }

    [Fact]
    public void EveryTableHasUuidKeyBranchAndTimestampColumns()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var context = CreateContext(connection);

        var entityTypes = DomainEntityTypes(context);
        Assert.Equal(ExpectedTableCount, entityTypes.Count);

        foreach (var entityType in entityTypes)
        {
            var table = entityType.GetTableName();
            Assert.False(string.IsNullOrWhiteSpace(table), $"{entityType.Name} is not mapped to a table");

            Assert.Equal(typeof(Guid), entityType.FindProperty(nameof(AuditedEntity.Id))?.ClrType);
            Assert.Equal(typeof(Guid), entityType.FindProperty(nameof(AuditedEntity.BranchId))?.ClrType);
            Assert.Equal(typeof(DateTimeOffset), entityType.FindProperty(nameof(AuditedEntity.CreatedAt))?.ClrType);
            Assert.Equal(typeof(DateTimeOffset), entityType.FindProperty(nameof(AuditedEntity.UpdatedAt))?.ClrType);
            Assert.Equal(typeof(DateTimeOffset?), entityType.FindProperty(nameof(AuditedEntity.DeletedAt))?.ClrType);

            var primaryKey = entityType.FindPrimaryKey();
            Assert.NotNull(primaryKey);
            Assert.Contains(primaryKey.Properties, property => property.Name == nameof(AuditedEntity.Id));
        }
    }

    [Fact]
    public void EveryTableExcludesSoftDeletedRowsByDefault()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var context = CreateContext(connection);

        foreach (var entityType in DomainEntityTypes(context))
        {
            var filters = entityType.GetDeclaredQueryFilters().ToList();
            Assert.NotEmpty(filters);
            Assert.Contains(
                filters,
                filter => filter.Expression!.ToString().Contains(
                    nameof(AuditedEntity.DeletedAt), StringComparison.Ordinal));
        }
    }

    [Fact]
    public void MoneyColumnsAreIntegerPiasters()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var context = CreateContext(connection);

        var moneyProperties = DomainEntityTypes(context)
            .SelectMany(type => type.GetProperties())
            .Where(property => property.Name.EndsWith("Piaster", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(moneyProperties);
        Assert.All(moneyProperties, property => Assert.Equal(typeof(long), property.ClrType));
        Assert.All(moneyProperties, property => Assert.Equal("INTEGER", property.GetColumnType()));

        var floatingPointProperties = DomainEntityTypes(context)
            .SelectMany(type => type.GetProperties())
            .Where(property => property.ClrType == typeof(float) || property.ClrType == typeof(double))
            .ToList();

        Assert.Empty(floatingPointProperties);
    }

    [Fact]
    public void ForeignKeysNeverCascadeDeletes()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var context = CreateContext(connection);

        var foreignKeys = DomainEntityTypes(context).SelectMany(type => type.GetForeignKeys()).ToList();
        Assert.NotEmpty(foreignKeys);
        Assert.All(foreignKeys, key => Assert.Equal(DeleteBehavior.Restrict, key.DeleteBehavior));
    }

    [Fact]
    public void ResultCorrectionsHaveSeparateAppendOnlyVersions()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var context = CreateContext(connection);

        var entityTypes = DomainEntityTypes(context);
        var result = entityTypes.Single(type => type.ClrType == typeof(Result));
        var version = entityTypes.Single(type => type.ClrType == typeof(ResultVersion));

        Assert.NotEqual(result.GetTableName(), version.GetTableName());
        Assert.False(version.FindProperty(nameof(ResultVersion.Reason))?.IsNullable);
        Assert.False(version.FindProperty(nameof(ResultVersion.ChangedAt))?.IsNullable);
        Assert.False(version.FindProperty(nameof(ResultVersion.ChangedByUserId))?.IsNullable);
    }

    [Fact]
    public void SampleBarcodesAreUniqueAcrossAllBranches()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var context = CreateContext(connection);

        var sample = DomainEntityTypes(context).Single(type => type.ClrType == typeof(Sample));
        var barcodeIndex = sample.GetIndexes()
            .Single(index => index.Properties.Any(property => property.Name == nameof(Sample.Barcode)));

        Assert.True(barcodeIndex.IsUnique);
        Assert.DoesNotContain(barcodeIndex.Properties, property => property.Name == nameof(AuditedEntity.BranchId));
    }

    [Fact]
    public void InitialMigrationAppliesToEmptyDatabaseAndSeedsNoClinicalValues()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var context = CreateContext(connection);

        context.Database.Migrate();
        context.Database.Migrate();

        var tableNames = context.Database.SqlQueryRaw<string>(
                """
                SELECT name FROM sqlite_master
                WHERE type = 'table'
                  AND name NOT LIKE 'sqlite%'
                  AND name <> '__EFMigrationsHistory'
                  AND name <> '__EFMigrationsLock'
                """)
            .ToList();

        Assert.True(
            tableNames.Count == ExpectedTableCount,
            $"Expected {ExpectedTableCount} tables but found: {string.Join(", ", tableNames.Order())}");

        var migrationId = Assert.Single(context.Database.GetMigrations());
        var migrationsAssembly = context.GetService<IMigrationsAssembly>();
        var migrationClass = migrationsAssembly.Migrations[migrationId];
        var migration = migrationsAssembly.CreateMigration(migrationClass, context.Database.ProviderName!);
        var operations = migration.UpOperations;

        Assert.DoesNotContain(operations, operation => operation is InsertDataOperation);
    }
}
