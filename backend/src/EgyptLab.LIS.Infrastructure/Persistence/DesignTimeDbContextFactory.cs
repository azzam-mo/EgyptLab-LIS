using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EgyptLab.LIS.Infrastructure.Persistence;

/// <summary>
/// Used only by EF tooling to generate migrations. The connection string is a
/// local placeholder, never a production secret.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LisDbContext>
{
    public LisDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LisDbContext>()
            .UseSqlite("Data Source=egyptlab-design.db")
            .Options;

        return new LisDbContext(options);
    }
}
