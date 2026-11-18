namespace EgyptLab.LIS.Infrastructure.Domain;

/// <summary>
/// An invoice for an order. All amounts are integer piasters.
/// </summary>
public sealed class Invoice : AuditedEntity
{
    public Guid OrderId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow;

    public long TotalPiaster { get; set; }

    public long DiscountPiaster { get; set; }

    public long NetPiaster { get; set; }

    public long PaidPiaster { get; set; }

    public string? ContractingParty { get; set; }

    public string? Status { get; set; }

    public string? CancellationReason { get; set; }

    public Order Order { get; set; } = null!;
}

/// <summary>
/// A payment or refund against an invoice. Cancellation is recorded rather
/// than deleting the row.
/// </summary>
public sealed class Payment : AuditedEntity
{
    public Guid InvoiceId { get; set; }

    public long AmountPiaster { get; set; }

    public DateTimeOffset PaidAt { get; set; } = DateTimeOffset.UtcNow;

    public string? Method { get; set; }

    public string? Reference { get; set; }

    public Guid? ReceivedByUserId { get; set; }

    public Guid? ReversalOfPaymentId { get; set; }

    public string? CancellationReason { get; set; }

    public DateTimeOffset? CancelledAt { get; set; }

    public Guid? CancelledByUserId { get; set; }

    public Invoice Invoice { get; set; } = null!;
}

/// <summary>
/// Append-only audit event. The base soft-delete column exists on every table
/// for uniformity, but audit rows are never deleted.
/// </summary>
public sealed class AuditLog : AuditedEntity
{
    public string EntityName { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public string Action { get; set; } = string.Empty;

    public Guid? ActorUserId { get; set; }

    public string? ActorRole { get; set; }

    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;

    public string? IpAddress { get; set; }

    public string? BeforeJson { get; set; }

    public string? AfterJson { get; set; }

    public string? Reason { get; set; }
}

/// <summary>
/// Transactional outbox designed now and implemented in Phase 5. Sequence is
/// assigned per branch by the outbox writer; no synchronization runs yet.
/// </summary>
public sealed class OutboxEvent : AuditedEntity
{
    public string AggregateType { get; set; } = string.Empty;

    public Guid AggregateId { get; set; }

    public string EventType { get; set; } = string.Empty;

    public long Sequence { get; set; }

    public string Payload { get; set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? PublishedAt { get; set; }

    public int RetryCount { get; set; }

    public string? LastError { get; set; }
}
