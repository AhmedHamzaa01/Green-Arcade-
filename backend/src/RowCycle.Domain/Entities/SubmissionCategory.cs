using RowCycle.Domain.Common;

namespace RowCycle.Domain.Entities;

public class SubmissionCategory : ISoftDeletable
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int PointsReward { get; set; }

    /// <summary>True for categories that record <see cref="Submission.WeightKg"/> (can collection, clean-up).</summary>
    public bool TracksWeight { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset? DeletedAt { get; set; }
}
