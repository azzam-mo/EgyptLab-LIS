namespace EgyptLab.LIS.Infrastructure.Domain;

/// <summary>
/// A test definition. Units, ranges and other clinical values are provided by
/// qualified laboratory staff; this schema stores no seeded clinical values.
/// </summary>
public sealed class LabTest : AuditedEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? NameEn { get; set; }

    public string? SampleType { get; set; }

    public string? Unit { get; set; }

    public string? ResultKind { get; set; }

    public long PricePiaster { get; set; }

    public int? TurnaroundMinutes { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// A group of tests sold together.
/// </summary>
public sealed class Panel : AuditedEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? NameEn { get; set; }

    public long PricePiaster { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Membership of a test in a panel.
/// </summary>
public sealed class PanelItem : AuditedEntity
{
    public Guid PanelId { get; set; }

    public Guid TestId { get; set; }

    public int SortOrder { get; set; }

    public Panel Panel { get; set; } = null!;

    public LabTest Test { get; set; } = null!;
}

/// <summary>
/// A configurable reference range with patient context. Bounds are columns
/// only; no age band, sex value, unit or limit is filled in by this phase.
/// </summary>
public sealed class ReferenceRange : AuditedEntity
{
    public Guid TestId { get; set; }

    public string? Component { get; set; }

    public string? SampleType { get; set; }

    public string? Sex { get; set; }

    public int? MinAge { get; set; }

    public int? MaxAge { get; set; }

    public string? AgeUnit { get; set; }

    public decimal? LowerBound { get; set; }

    public decimal? UpperBound { get; set; }

    public bool LowerInclusive { get; set; } = true;

    public bool UpperInclusive { get; set; } = true;

    public string? NormalText { get; set; }

    public decimal? CriticalLower { get; set; }

    public decimal? CriticalUpper { get; set; }

    public bool IsActive { get; set; } = true;

    public Guid? ApprovedByUserId { get; set; }

    public DateTimeOffset? ApprovedAt { get; set; }

    public LabTest Test { get; set; } = null!;
}
