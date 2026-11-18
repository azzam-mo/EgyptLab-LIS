using System.Linq.Expressions;
using EgyptLab.LIS.Infrastructure.Domain;
using Microsoft.EntityFrameworkCore;

namespace EgyptLab.LIS.Infrastructure.Persistence;

/// <summary>
/// Local branch database. All writes go through this context so audit and
/// branch-scoped authorization stay in one place.
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

        // The soft-delete filter is configured in AuditedEntityConfiguration.
        // Branch scope is added here so the current request value is read at
        // query time and cannot leak through the EF model cache.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(type => type.ClrType is not null
                         && typeof(AuditedEntity).IsAssignableFrom(type.ClrType)))
        {
            var parameter = Expression.Parameter(entityType.ClrType!, "entity");
            var branchProperty = Expression.Property(parameter, nameof(AuditedEntity.BranchId));
            var currentBranch = Expression.Property(
                null,
                typeof(BranchScope),
                nameof(BranchScope.CurrentBranchId));
            var comparison = Expression.Equal(Expression.Convert(branchProperty, typeof(Guid?)), currentBranch);
            var filter = Expression.Lambda(comparison, parameter);

            modelBuilder.Entity(entityType.ClrType!).HasQueryFilter(QueryFilterKeys.BranchScope, filter);
        }
    }
}
