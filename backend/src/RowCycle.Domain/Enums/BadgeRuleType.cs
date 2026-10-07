namespace RowCycle.Domain.Enums;

/// <summary>The badge rules from PRD F4.</summary>
public enum BadgeRuleType
{
    /// <summary>Total approved submissions &gt;= threshold.</summary>
    TotalApproved,

    /// <summary>Approved submissions in <c>RuleCategoryId</c> &gt;= threshold.</summary>
    CategoryApproved,

    /// <summary>First order placed.</summary>
    FirstOrder,
}
