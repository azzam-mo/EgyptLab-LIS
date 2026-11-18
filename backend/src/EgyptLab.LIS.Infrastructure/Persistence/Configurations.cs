using EgyptLab.LIS.Infrastructure.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgyptLab.LIS.Infrastructure.Persistence;

/// <summary>
/// Applies the columns required on every table. The query filter is the
/// database-level expression of the soft-delete rule.
/// </summary>
public abstract class AuditedEntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : AuditedEntity
{
    protected abstract string TableName { get; }

    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.BranchId).IsRequired();
        builder.Property(entity => entity.CreatedAt).IsRequired();
        builder.Property(entity => entity.UpdatedAt).IsRequired();
        builder.Property(entity => entity.DeletedAt);
        builder.HasQueryFilter(entity => entity.DeletedAt == null);
    }
}

public sealed class BranchConfiguration : AuditedEntityConfiguration<Branch>
{
    protected override string TableName => "branches";

    public override void Configure(EntityTypeBuilder<Branch> builder)
    {
        base.Configure(builder);
        builder.Property(entity => entity.Code).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(entity => entity.Code).IsUnique();
    }
}

public sealed class RoleConfiguration : AuditedEntityConfiguration<Role>
{
    protected override string TableName => "roles";

    public override void Configure(EntityTypeBuilder<Role> builder)
    {
        base.Configure(builder);
        builder.Property(entity => entity.Name).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(500);
        builder.HasIndex(entity => new { entity.BranchId, entity.Name }).IsUnique();
    }
}

public sealed class UserConfiguration : AuditedEntityConfiguration<User>
{
    protected override string TableName => "users";

    public override void Configure(EntityTypeBuilder<User> builder)
    {
        base.Configure(builder);
        builder.Property(entity => entity.UserName).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.FullName).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.Email).HasMaxLength(200);
        builder.Property(entity => entity.Phone).HasMaxLength(32);
        builder.HasIndex(entity => new { entity.BranchId, entity.UserName }).IsUnique();
    }
}

public sealed class UserRoleConfiguration : AuditedEntityConfiguration<UserRole>
{
    protected override string TableName => "user_roles";

    public override void Configure(EntityTypeBuilder<UserRole> builder)
    {
        base.Configure(builder);
        builder.HasOne(entity => entity.User).WithMany().HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.Role).WithMany().HasForeignKey(entity => entity.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.UserId, entity.RoleId }).IsUnique();
    }
}

public sealed class PatientConfiguration : AuditedEntityConfiguration<Patient>
{
    protected override string TableName => "patients";

    public override void Configure(EntityTypeBuilder<Patient> builder)
    {
        base.Configure(builder);
        builder.Property(entity => entity.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.LastName).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.NationalId).HasMaxLength(32);
        builder.Property(entity => entity.Sex).HasMaxLength(16);
        builder.Property(entity => entity.Phone).HasMaxLength(32);
        builder.HasIndex(entity => new { entity.BranchId, entity.NationalId }).IsUnique();
    }
}

public sealed class LabTestConfiguration : AuditedEntityConfiguration<LabTest>
{
    protected override string TableName => "tests";

    public override void Configure(EntityTypeBuilder<LabTest> builder)
    {
        base.Configure(builder);
        builder.Property(entity => entity.Code).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.NameEn).HasMaxLength(200);
        builder.Property(entity => entity.SampleType).HasMaxLength(64);
        builder.Property(entity => entity.Unit).HasMaxLength(64);
        builder.Property(entity => entity.ResultKind).HasMaxLength(32);
        builder.HasIndex(entity => new { entity.BranchId, entity.Code }).IsUnique();
    }
}

public sealed class PanelConfiguration : AuditedEntityConfiguration<Panel>
{
    protected override string TableName => "panels";

    public override void Configure(EntityTypeBuilder<Panel> builder)
    {
        base.Configure(builder);
        builder.Property(entity => entity.Code).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.NameEn).HasMaxLength(200);
        builder.HasIndex(entity => new { entity.BranchId, entity.Code }).IsUnique();
    }
}

public sealed class PanelItemConfiguration : AuditedEntityConfiguration<PanelItem>
{
    protected override string TableName => "panel_items";

    public override void Configure(EntityTypeBuilder<PanelItem> builder)
    {
        base.Configure(builder);
        builder.HasOne(entity => entity.Panel).WithMany().HasForeignKey(entity => entity.PanelId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.Test).WithMany().HasForeignKey(entity => entity.TestId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.PanelId, entity.TestId }).IsUnique();
    }
}

public sealed class ReferenceRangeConfiguration : AuditedEntityConfiguration<ReferenceRange>
{
    protected override string TableName => "reference_ranges";

    public override void Configure(EntityTypeBuilder<ReferenceRange> builder)
    {
        base.Configure(builder);
        builder.Property(entity => entity.Component).HasMaxLength(64);
        builder.Property(entity => entity.SampleType).HasMaxLength(64);
        builder.Property(entity => entity.Sex).HasMaxLength(16);
        builder.Property(entity => entity.AgeUnit).HasMaxLength(16);
        builder.Property(entity => entity.NormalText).HasMaxLength(200);
        builder.HasOne(entity => entity.Test).WithMany().HasForeignKey(entity => entity.TestId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.TestId, entity.Component, entity.Sex });
    }
}

public sealed class OrderConfiguration : AuditedEntityConfiguration<Order>
{
    protected override string TableName => "orders";

    public override void Configure(EntityTypeBuilder<Order> builder)
    {
        base.Configure(builder);
        builder.Property(entity => entity.OrderNumber).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.Status).HasMaxLength(32);
        builder.Property(entity => entity.ReferredBy).HasMaxLength(200);
        builder.HasOne(entity => entity.Patient).WithMany().HasForeignKey(entity => entity.PatientId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.BranchId, entity.OrderNumber }).IsUnique();
        builder.HasIndex(entity => entity.PatientId);
    }
}

public sealed class OrderItemConfiguration : AuditedEntityConfiguration<OrderItem>
{
    protected override string TableName => "order_items";

    public override void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        base.Configure(builder);
        builder.HasOne(entity => entity.Order).WithMany().HasForeignKey(entity => entity.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.Test).WithMany().HasForeignKey(entity => entity.TestId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.Panel).WithMany().HasForeignKey(entity => entity.PanelId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.OrderId);
    }
}

public sealed class SampleConfiguration : AuditedEntityConfiguration<Sample>
{
    protected override string TableName => "samples";

    public override void Configure(EntityTypeBuilder<Sample> builder)
    {
        base.Configure(builder);
        builder.Property(entity => entity.Barcode).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.SampleType).HasMaxLength(64);
        builder.Property(entity => entity.Status).HasMaxLength(32);
        builder.Property(entity => entity.RejectionReason).HasMaxLength(500);
        builder.HasOne(entity => entity.Order).WithMany().HasForeignKey(entity => entity.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.OrderItem).WithMany().HasForeignKey(entity => entity.OrderItemId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.Barcode).IsUnique();
        builder.HasIndex(entity => entity.OrderId);
        builder.HasIndex(entity => entity.OrderItemId);
    }
}

public sealed class ResultConfiguration : AuditedEntityConfiguration<Result>
{
    protected override string TableName => "results";

    public override void Configure(EntityTypeBuilder<Result> builder)
    {
        base.Configure(builder);
        builder.Property(entity => entity.ValueText).HasMaxLength(500);
        builder.Property(entity => entity.UnitSnapshot).HasMaxLength(64);
        builder.Property(entity => entity.Status).HasMaxLength(32);
        builder.HasOne(entity => entity.Sample).WithMany().HasForeignKey(entity => entity.SampleId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.OrderItem).WithMany().HasForeignKey(entity => entity.OrderItemId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.SampleId, entity.OrderItemId });
    }
}

public sealed class ResultVersionConfiguration : AuditedEntityConfiguration<ResultVersion>
{
    protected override string TableName => "result_versions";

    public override void Configure(EntityTypeBuilder<ResultVersion> builder)
    {
        base.Configure(builder);
        builder.Property(entity => entity.ValueText).HasMaxLength(500);
        builder.Property(entity => entity.UnitSnapshot).HasMaxLength(64);
        builder.Property(entity => entity.Status).HasMaxLength(32);
        builder.Property(entity => entity.Reason).HasMaxLength(500).IsRequired();
        builder.HasOne(entity => entity.Result).WithMany().HasForeignKey(entity => entity.ResultId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ResultVersion>().WithMany().HasForeignKey(entity => entity.PriorVersionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.ResultId, entity.VersionNumber }).IsUnique();
    }
}

public sealed class InvoiceConfiguration : AuditedEntityConfiguration<Invoice>
{
    protected override string TableName => "invoices";

    public override void Configure(EntityTypeBuilder<Invoice> builder)
    {
        base.Configure(builder);
        builder.Property(entity => entity.InvoiceNumber).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.ContractingParty).HasMaxLength(200);
        builder.Property(entity => entity.Status).HasMaxLength(32);
        builder.Property(entity => entity.CancellationReason).HasMaxLength(500);
        builder.HasOne(entity => entity.Order).WithMany().HasForeignKey(entity => entity.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.BranchId, entity.InvoiceNumber }).IsUnique();
        builder.HasIndex(entity => entity.OrderId);
    }
}

public sealed class PaymentConfiguration : AuditedEntityConfiguration<Payment>
{
    protected override string TableName => "payments";

    public override void Configure(EntityTypeBuilder<Payment> builder)
    {
        base.Configure(builder);
        builder.Property(entity => entity.Method).HasMaxLength(32);
        builder.Property(entity => entity.Reference).HasMaxLength(100);
        builder.Property(entity => entity.CancellationReason).HasMaxLength(500);
        builder.HasOne(entity => entity.Invoice).WithMany().HasForeignKey(entity => entity.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Payment>().WithMany().HasForeignKey(entity => entity.ReversalOfPaymentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.InvoiceId);
        builder.HasIndex(entity => entity.ReversalOfPaymentId);
    }
}

public sealed class AuditLogConfiguration : AuditedEntityConfiguration<AuditLog>
{
    protected override string TableName => "audit_log";

    public override void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        base.Configure(builder);
        builder.Property(entity => entity.EntityName).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.Action).HasMaxLength(50).IsRequired();
        builder.Property(entity => entity.ActorRole).HasMaxLength(100);
        builder.Property(entity => entity.IpAddress).HasMaxLength(64);
        builder.Property(entity => entity.Reason).HasMaxLength(500);
        builder.HasIndex(entity => new { entity.EntityName, entity.EntityId });
        builder.HasIndex(entity => entity.OccurredAt);
    }
}

public sealed class OutboxEventConfiguration : AuditedEntityConfiguration<OutboxEvent>
{
    protected override string TableName => "outbox_events";

    public override void Configure(EntityTypeBuilder<OutboxEvent> builder)
    {
        base.Configure(builder);
        builder.Property(entity => entity.AggregateType).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.EventType).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.Payload).IsRequired();
        builder.Property(entity => entity.LastError).HasMaxLength(1000);
        builder.HasIndex(entity => new { entity.BranchId, entity.Sequence, entity.PublishedAt });
    }
}
