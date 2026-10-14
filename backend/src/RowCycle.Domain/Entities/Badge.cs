using RowCycle.Domain.Common;
using RowCycle.Domain.Enums;

namespace RowCycle.Domain.Entities;

public class Badge : ISoftDeletable
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>A Material icon name from <see cref="Constants.BadgeIcons.All"/>.</summary>
    public string Icon { get; set; } = Constants.BadgeIcons.Default;
    public BadgeRuleType RuleType { get; set; }

    /// <summary>Set only for <see cref="BadgeRuleType.CategoryApproved"/>.</summary>
    public Guid? RuleCategoryId { get; set; }

    public int Threshold { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}
