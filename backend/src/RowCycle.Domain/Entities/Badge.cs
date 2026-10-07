using RowCycle.Domain.Enums;

namespace RowCycle.Domain.Entities;

public class Badge
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public BadgeRuleType RuleType { get; set; }

    /// <summary>Set only for <see cref="BadgeRuleType.CategoryApproved"/>.</summary>
    public Guid? RuleCategoryId { get; set; }

    public int Threshold { get; set; }
}
