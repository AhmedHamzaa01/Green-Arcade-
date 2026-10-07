namespace RowCycle.Domain.Entities;

public class UserBadge
{
    public Guid UserId { get; set; }
    public Guid BadgeId { get; set; }
    public Badge? Badge { get; set; }
    public DateTimeOffset AwardedAt { get; set; }
}
