using EgyptLab.LIS.Infrastructure.Domain;
using Microsoft.EntityFrameworkCore;

namespace EgyptLab.LIS.Infrastructure.Persistence;

/// <summary>
/// Local branch database. All writes go through this context so later phases
/// can add audit interception and branch-scoped authorization in one place.
/// </summary>
public sealed class LisDbContext(DbContextOptions<LisDbContext> options) : DbContext(options)
{
    public DbSet<Branch> Branches => Set<Branch>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<User> Users => Set<User>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<Patient> Patients => Set<Patient>();

    public DbSet<LabTest> LabTests => Set<LabTest>();

    public DbSet<Panel> Panels => Set<Panel>();

    public DbSet<PanelItem> PanelItems => Set<PanelItem>();

    public DbSet<ReferenceRange> ReferenceRanges => Set<ReferenceRange>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<Sample> Samples => Set<Sample>();

    public DbSet<Result> Results => Set<Result>();

    public DbSet<ResultVersion> ResultVersions => Set<ResultVersion>();

    public DbSet<Invoice> Invoices => Set<Invoice>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<AuditLog> AuditLog => Set<AuditLog>();

    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LisDbContext).Assembly);
    }
}
