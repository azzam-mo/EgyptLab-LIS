namespace EgyptLab.LIS.Infrastructure.Domain;

/// <summary>
/// A patient record. National ID validation and duplicate detection are
/// Phase 1 rules; no real patient data is stored in this repository.
/// </summary>
public sealed class Patient : AuditedEntity
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? NationalId { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Sex { get; set; }

    public string? Phone { get; set; }
}

/// <summary>
/// A set of requested tests for one patient in one visit.
/// </summary>
public sealed class Order : AuditedEntity
{
    public Guid PatientId { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public DateTimeOffset OrderedAt { get; set; } = DateTimeOffset.UtcNow;

    public string? Status { get; set; }

    public string? ReferredBy { get; set; }

    public long TotalPiaster { get; set; }

    public long DiscountPiaster { get; set; }

    public Patient Patient { get; set; } = null!;
}

/// <summary>
/// One ordered test or panel. The unit price is a snapshot so later catalogue
/// price changes cannot alter an existing order.
/// </summary>
public sealed class OrderItem : AuditedEntity
{
    public Guid OrderId { get; set; }

    public Guid TestId { get; set; }

    public Guid? PanelId { get; set; }

    public int Quantity { get; set; } = 1;

    public long UnitPricePiaster { get; set; }

    public long LineTotalPiaster { get; set; }

    public Order Order { get; set; } = null!;

    public LabTest Test { get; set; } = null!;

    public Panel? Panel { get; set; }
}

/// <summary>
/// A physical specimen. The barcode is globally unique so it can be scanned
/// at any branch before synchronization.
/// </summary>
public sealed class Sample : AuditedEntity
{
    public Guid OrderId { get; set; }

    public Guid? OrderItemId { get; set; }

    public string Barcode { get; set; } = string.Empty;

    public string? SampleType { get; set; }

    public DateTimeOffset? CollectedAt { get; set; }

    public DateTimeOffset? ReceivedAt { get; set; }

    public string? Status { get; set; }

    public Guid? CollectedByUserId { get; set; }

    public string? RejectionReason { get; set; }

    public Order Order { get; set; } = null!;

    public OrderItem? OrderItem { get; set; }
}

/// <summary>
/// Current state of a result. Approval state transitions and permissions are
/// implemented in Phase 2; no status values are hard-coded in this phase.
/// </summary>
public sealed class Result : AuditedEntity
{
    public Guid SampleId { get; set; }

    public Guid OrderItemId { get; set; }

    public decimal? ValueNumeric { get; set; }

    public string? ValueText { get; set; }

    public string? UnitSnapshot { get; set; }

    public string? Status { get; set; }

    public Guid? EnteredByUserId { get; set; }

    public DateTimeOffset? EnteredAt { get; set; }

    public Guid? ReviewedByUserId { get; set; }

    public DateTimeOffset? ReviewedAt { get; set; }

    public Guid? ApprovedByUserId { get; set; }

    public DateTimeOffset? ApprovedAt { get; set; }

    public DateTimeOffset? ReleasedAt { get; set; }

    public Guid? CriticalConfirmedByUserId { get; set; }

    public DateTimeOffset? CriticalConfirmedAt { get; set; }

    public int CurrentVersion { get; set; } = 1;

    public Sample Sample { get; set; } = null!;

    public OrderItem OrderItem { get; set; } = null!;
}

/// <summary>
/// Append-only version of a result. Every correction creates a new row with
/// a mandatory reason; earlier rows are never overwritten.
/// </summary>
public sealed class ResultVersion : AuditedEntity
{
    public Guid ResultId { get; set; }

    public int VersionNumber { get; set; }

    public decimal? ValueNumeric { get; set; }

    public string? ValueText { get; set; }

    public string? UnitSnapshot { get; set; }

    public string? Status { get; set; }

    public string Reason { get; set; } = string.Empty;

    public Guid ChangedByUserId { get; set; }

    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;

    public Guid? PriorVersionId { get; set; }

    public Result Result { get; set; } = null!;
}
