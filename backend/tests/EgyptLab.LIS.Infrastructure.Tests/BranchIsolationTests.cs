using EgyptLab.LIS.Infrastructure.Domain;
using EgyptLab.LIS.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EgyptLab.LIS.Infrastructure.Tests;

/// <summary>
/// Branch isolation is enforced by the database query layer, not by callers
/// remembering to add a Where clause.
/// </summary>
public sealed class BranchIsolationTests
{
    [Fact]
    public async Task QueriesOnlyReturnRowsFromTheCurrentBranch()
    {
        // Shared-cache in-memory database: each context gets its own connection,
        // as it does per HTTP request.
        var databaseName = $"branch-{Guid.NewGuid():N}";
        var connectionString = $"Data Source=file:{databaseName}?mode=memory&cache=shared";
        using var anchor = new SqliteConnection(connectionString);
        anchor.Open();

        var optionsBuilder = new DbContextOptionsBuilder<LisDbContext>();
        optionsBuilder.UseSqlite(connectionString);
        optionsBuilder.UseBranchScope();
        var options = optionsBuilder.Options;

        var branchA = new Branch { Code = "A", Name = "Synthetic Branch A" };
        var branchB = new Branch { Code = "B", Name = "Synthetic Branch B" };
        Assert.Equal(branchA.Id, branchA.BranchId);
        Assert.Equal(branchB.Id, branchB.BranchId);

        using (var seedContext = new LisDbContext(options))
        {
            seedContext.Database.Migrate();
            seedContext.Branches.AddRange(branchA, branchB);
            seedContext.Patients.AddRange(
                new Patient
                {
                    BranchId = branchA.Id,
                    FirstName = "Synthetic",
                    LastName = "Patient-A"
                },
                new Patient
                {
                    BranchId = branchB.Id,
                    FirstName = "Synthetic",
                    LastName = "Patient-B"
                });
            await seedContext.SaveChangesAsync();
        }

        var scope = new BranchScope();
        try
        {
            scope.Set(branchA.Id);
            using (var contextA = new LisDbContext(options))
            {
                var rowA = Assert.Single(await contextA.Patients.AsNoTracking().ToListAsync());
                Assert.Equal("Patient-A", rowA.LastName);
            }

            scope.Set(branchB.Id);
            using (var contextB = new LisDbContext(options))
            {
                var rowB = Assert.Single(await contextB.Patients.AsNoTracking().ToListAsync());
                Assert.Equal("Patient-B", rowB.LastName);
            }

            scope.Clear();
            Assert.Null(BranchScope.CurrentBranchId);
            using var contextNoScope = new LisDbContext(options);
            Assert.Empty(await contextNoScope.Patients.AsNoTracking().ToListAsync());
        }
        finally
        {
            scope.Clear();
        }
    }
}
