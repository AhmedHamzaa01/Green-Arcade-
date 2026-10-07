using RowCycle.Domain.Enums;

namespace RowCycle.Domain.Entities;

public class Submission
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid CategoryId { get; set; }
    public SubmissionCategory? Category { get; set; }
    public string PhotoUrl { get; set; } = string.Empty;
    public string? Note { get; set; }

    /// <summary>Only for categories that track weight; feeds the future Rotahope tracker.</summary>
    public decimal? WeightKg { get; set; }

    public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;
    public Guid? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? RejectReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
