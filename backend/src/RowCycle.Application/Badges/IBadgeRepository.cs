using RowCycle.Domain.Entities;
using RowCycle.Domain.Enums;

namespace RowCycle.Application.Badges;

/// <summary>A badge with its rule category's name (null unless the rule is per category).</summary>
public sealed record BadgeView(Badge Badge, string? RuleCategoryName);

/// <summary>A badge the member earned, with when.</summary>
public sealed record EarnedBadge(Badge Badge, DateTimeOffset AwardedAt);

public interface IBadgeRepository
{
    /// <summary>Badges that aren't deleted, by name.</summary>
    Task<IReadOnlyList<BadgeView>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Tracked, for editing. Null if missing or deleted.</summary>
    Task<Badge?> FindAsync(Guid badgeId, CancellationToken cancellationToken = default);

    Task<bool> NameTakenAsync(string name, Guid? exceptId, CancellationToken cancellationToken = default);

    /// <summary>Whether a badge that isn't deleted uses the category in its rule.</summary>
    Task<bool> IsCategoryUsedAsync(Guid categoryId, CancellationToken cancellationToken = default);

    void Add(Badge badge);

    /// <summary>Soft delete; members keep the badge if they earned it.</summary>
    void Remove(Badge badge);

    // ---------- Awarding ----------

    /// <summary>
    /// Locks the member's profile row until the transaction ends, so two awards for the same member run one after the other
    /// and each sees the other's result (FR-12).
    /// </summary>
    Task LockMemberAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Badges that aren't deleted, use one of <paramref name="ruleTypes"/>, and the member doesn't have yet.</summary>
    Task<IReadOnlyList<Badge>> ListNotEarnedAsync(
        Guid userId, IReadOnlyCollection<BadgeRuleType> ruleTypes, CancellationToken cancellationToken = default);

    /// <summary>Approved submissions of the member, all or in one category.</summary>
    Task<int> CountApprovedAsync(Guid userId, Guid? categoryId, CancellationToken cancellationToken = default);

    Task<bool> HasDeliveredOrderAsync(Guid userId, CancellationToken cancellationToken = default);

    void Award(UserBadge award);

    /// <summary>The member's badges, newest first, including badges deleted since they were earned.</summary>
    Task<IReadOnlyList<EarnedBadge>> ListEarnedAsync(Guid userId, CancellationToken cancellationToken = default);
}
