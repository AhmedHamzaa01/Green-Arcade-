using RowCycle.Domain.Common;
using RowCycle.Domain.Enums;

namespace RowCycle.Domain.Entities;

/// <summary>One append-only row per points change (FR-05). Never updated or deleted.</summary>
public class PointsLedgerEntry : INonDeletable
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>Signed change: positive for Earn, negative for Redeem; Reverse and Adjust can be either.</summary>
    public int Amount { get; set; }

    public PointsEntryType Type { get; set; }
    public PointsSourceType SourceType { get; set; }
    public Guid? SourceId { get; set; }
    public string? Reason { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
