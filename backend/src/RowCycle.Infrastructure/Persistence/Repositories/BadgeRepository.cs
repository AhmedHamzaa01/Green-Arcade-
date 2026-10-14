using Microsoft.EntityFrameworkCore;
using RowCycle.Application.Badges;
using RowCycle.Application.Common;
using RowCycle.Domain.Entities;
using RowCycle.Domain.Enums;

namespace RowCycle.Infrastructure.Persistence.Repositories;

internal sealed class BadgeRepository(AppDbContext db) : IBadgeRepository
{
    public async Task<IReadOnlyList<BadgeView>> ListAsync(CancellationToken cancellationToken = default) =>
        // IgnoreQueryFilters turns the soft-delete filter off for the whole query (to show a deleted rule category's name),
        // so deleted badges are filtered out by hand.
        await (from b in db.Badges.AsNoTracking().IgnoreQueryFilters()
               where b.DeletedAt == null
               join c in db.SubmissionCategories.IgnoreQueryFilters() on b.RuleCategoryId equals c.Id into categories
               from c in categories.DefaultIfEmpty()
               orderby b.Name
               select new BadgeView(b, c == null ? null : c.Name))
            .ToListAsync(cancellationToken);

    public Task<Badge?> FindAsync(Guid badgeId, CancellationToken cancellationToken = default) =>
        db.Badges.SingleOrDefaultAsync(b => b.Id == badgeId, cancellationToken);

    public Task<bool> NameTakenAsync(string name, Guid? exceptId, CancellationToken cancellationToken = default) =>
        db.Badges.AnyAsync(b => b.Id != exceptId && b.Name.ToLower() == name.ToLower(), cancellationToken);

    public Task<bool> IsCategoryUsedAsync(Guid categoryId, CancellationToken cancellationToken = default) =>
        db.Badges.AnyAsync(b => b.RuleCategoryId == categoryId, cancellationToken);

    public void Add(Badge badge) => db.Badges.Add(badge);

    public void Remove(Badge badge) => db.Badges.Remove(badge);

    public async Task LockMemberAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("Rows can only be locked inside a transaction.");
        }

        // Plain SQL because LINQ can't say FOR UPDATE. Only the lock matters, so nothing is loaded.
        var locked = await db.Database
            .SqlQuery<Guid>($"SELECT user_id AS \"Value\" FROM user_profiles WHERE user_id = {userId} FOR UPDATE")
            .ToListAsync(cancellationToken);
        if (locked.Count == 0)
        {
            throw AppException.NotFound("User not found.");
        }
    }

    public async Task<IReadOnlyList<Badge>> ListNotEarnedAsync(
        Guid userId, IReadOnlyCollection<BadgeRuleType> ruleTypes, CancellationToken cancellationToken = default) =>
        await db.Badges.AsNoTracking()
            .Where(b => ruleTypes.Contains(b.RuleType))
            .Where(b => !db.UserBadges.Any(ub => ub.UserId == userId && ub.BadgeId == b.Id))
            .ToListAsync(cancellationToken);

    public Task<int> CountApprovedAsync(Guid userId, Guid? categoryId, CancellationToken cancellationToken = default) =>
        db.Submissions.CountAsync(
            s => s.UserId == userId && s.Status == SubmissionStatus.Approved && (categoryId == null || s.CategoryId == categoryId),
            cancellationToken);

    public Task<bool> HasDeliveredOrderAsync(Guid userId, CancellationToken cancellationToken = default) =>
        db.Orders.AnyAsync(o => o.UserId == userId && o.Status == OrderStatus.Delivered, cancellationToken);

    public void Award(UserBadge award) => db.UserBadges.Add(award);

    public async Task<IReadOnlyList<EarnedBadge>> ListEarnedAsync(Guid userId, CancellationToken cancellationToken = default) =>
        // IgnoreQueryFilters: a badge deleted after it was earned still shows on the member's profile.
        await db.UserBadges.AsNoTracking().IgnoreQueryFilters()
            .Where(ub => ub.UserId == userId)
            .OrderByDescending(ub => ub.AwardedAt)
            .Select(ub => new EarnedBadge(ub.Badge!, ub.AwardedAt))
            .ToListAsync(cancellationToken);
}
