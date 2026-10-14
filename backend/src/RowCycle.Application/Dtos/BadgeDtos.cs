using RowCycle.Domain.Enums;

namespace RowCycle.Application.Dtos;

/// <summary>A badge as admins see it. <c>RuleCategoryName</c> only for the "in a category" rule.</summary>
public sealed record BadgeResponse(
    Guid Id,
    string Name,
    string Icon,
    BadgeRuleType RuleType,
    Guid? RuleCategoryId,
    string? RuleCategoryName,
    int Threshold);

/// <summary>
/// Create or edit a badge. <c>RuleCategoryId</c> only for <see cref="BadgeRuleType.CategoryApproved"/>;
/// <c>Threshold</c> is ignored (always 1) for <see cref="BadgeRuleType.FirstOrder"/>.
/// </summary>
public sealed record SaveBadgeRequest(string Name, string Icon, BadgeRuleType RuleType, Guid? RuleCategoryId, int Threshold);

/// <summary>A badge the member earned (F4).</summary>
public sealed record MyBadgeResponse(Guid Id, string Name, string Icon, DateTimeOffset AwardedAt);
