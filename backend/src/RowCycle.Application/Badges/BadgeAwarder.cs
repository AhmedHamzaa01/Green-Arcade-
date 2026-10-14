using RowCycle.Application.Common;
using RowCycle.Domain.Entities;
using RowCycle.Domain.Enums;

namespace RowCycle.Application.Badges;

/// <summary>What just happened to the member, so only the rules it can affect are checked.</summary>
internal enum BadgeTrigger
{
    SubmissionApproved,
    OrderDelivered,
}

/// <summary>
/// Gives the member every badge they have now earned and don't have yet (FR-12). Must run inside the caller's
/// transaction (approval or delivery), so the badge is saved together with the change that earned it, or not at all.
/// </summary>
internal sealed class BadgeAwarder(IBadgeRepository badges, IUnitOfWork unitOfWork, TimeProvider timeProvider)
{
    private static readonly BadgeRuleType[] SubmissionRules = [BadgeRuleType.TotalApproved, BadgeRuleType.CategoryApproved];
    private static readonly BadgeRuleType[] OrderRules = [BadgeRuleType.FirstOrder];

    public async Task AwardAsync(Guid userId, BadgeTrigger trigger, CancellationToken cancellationToken)
    {
        // Save the approval/delivery first (still uncommitted) so the counts below include it.
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await badges.LockMemberAsync(userId, cancellationToken);

        var candidates = await badges.ListNotEarnedAsync(
            userId, trigger == BadgeTrigger.SubmissionApproved ? SubmissionRules : OrderRules, cancellationToken);

        foreach (var badge in candidates)
        {
            if (await IsEarnedAsync(userId, badge, cancellationToken))
            {
                badges.Award(new UserBadge { UserId = userId, BadgeId = badge.Id, AwardedAt = timeProvider.GetUtcNow() });
            }
        }
    }

    private async Task<bool> IsEarnedAsync(Guid userId, Badge badge, CancellationToken cancellationToken) => badge.RuleType switch
    {
        BadgeRuleType.TotalApproved => await badges.CountApprovedAsync(userId, null, cancellationToken) >= badge.Threshold,
        BadgeRuleType.CategoryApproved => badge.RuleCategoryId is { } categoryId
            && await badges.CountApprovedAsync(userId, categoryId, cancellationToken) >= badge.Threshold,
        BadgeRuleType.FirstOrder => await badges.HasDeliveredOrderAsync(userId, cancellationToken),
        _ => false,
    };
}
