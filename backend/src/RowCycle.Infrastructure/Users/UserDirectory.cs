using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Application.Users;
using RowCycle.Domain.Enums;
using RowCycle.Infrastructure.Identity;
using RowCycle.Infrastructure.Persistence;

namespace RowCycle.Infrastructure.Users;

internal sealed class UserDirectory(AppDbContext db, UserManager<AppUser> userManager) : IUserDirectory
{
    public async Task<PagedResult<AdminUserListItem>> SearchAsync(
        UserQuery query, PageRequest page, CancellationToken cancellationToken = default)
    {
        var rows = from u in db.Users.AsNoTracking()
                   join p in db.UserProfiles on u.Id equals p.UserId
                   select new { User = u, Profile = p };

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = "%" + query.Search.Trim().Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_") + "%";
            rows = rows.Where(r => EF.Functions.ILike(r.Profile.FullName, pattern, @"\") || EF.Functions.ILike(r.User.Email!, pattern, @"\"));
        }

        if (query.Role is { } role)
        {
            rows = rows.Where(r => db.UserRoles.Any(ur => ur.UserId == r.User.Id
                && db.Roles.Any(x => x.Id == ur.RoleId && x.Name == role)));
        }

        if (query.Blocked is { } blocked)
        {
            rows = rows.Where(r => r.User.IsActive == !blocked);
        }

        // Ids are version-7 GUIDs (time-ordered), so ordering by id puts the newest accounts first.
        var total = await rows.CountAsync(cancellationToken);
        var items = await rows
            .OrderByDescending(r => r.User.Id)
            .Skip(page.Skip).Take(page.PageSize)
            .Select(r => new { r.User.Id, r.Profile.FullName, r.User.Email, r.User.IsActive, r.User.EmailConfirmed, r.Profile.PointsBalance })
            .ToListAsync(cancellationToken);

        var roles = await RolesOfAsync(items.Select(i => i.Id).ToList(), cancellationToken);
        return new PagedResult<AdminUserListItem>(
            items.Select(i => new AdminUserListItem(
                i.Id, i.FullName, i.Email!, roles.GetValueOrDefault(i.Id, []), i.IsActive, i.EmailConfirmed, i.PointsBalance)).ToList(),
            page.Page,
            page.PageSize,
            total);
    }

    public async Task<AdminUserDetail?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await (from u in db.Users.AsNoTracking()
                          join p in db.UserProfiles on u.Id equals p.UserId
                          where u.Id == userId
                          select new
                          {
                              u.Id,
                              p.FullName,
                              u.Email,
                              p.Phone,
                              u.IsActive,
                              u.EmailConfirmed,
                              p.PointsBalance,
                              Approved = db.Submissions.Count(s => s.UserId == u.Id && s.Status == SubmissionStatus.Approved),
                              Pending = db.Submissions.Count(s => s.UserId == u.Id && s.Status == SubmissionStatus.Pending),
                              Orders = db.Orders.Count(o => o.UserId == u.Id),
                              Badges = db.UserBadges.Count(b => b.UserId == u.Id),
                          })
            .SingleOrDefaultAsync(cancellationToken);
        if (user is null)
        {
            return null;
        }

        var roles = await GetRolesAsync(userId, cancellationToken);
        return new AdminUserDetail(user.Id, user.FullName, user.Email!, user.Phone, roles, user.IsActive, user.EmailConfirmed,
            user.PointsBalance, user.Approved, user.Pending, user.Orders, user.Badges);
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken cancellationToken = default) =>
        (await RolesOfAsync([userId], cancellationToken)).GetValueOrDefault(userId, []);

    public async Task SetRolesAsync(Guid userId, IReadOnlyCollection<string> roles, CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(userId);
        var current = await userManager.GetRolesAsync(user);

        Check(await userManager.RemoveFromRolesAsync(user, current.Except(roles)));
        Check(await userManager.AddToRolesAsync(user, roles.Except(current)));
        Check(await userManager.UpdateSecurityStampAsync(user));
    }

    public async Task SetActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(userId);
        user.IsActive = isActive;
        Check(await userManager.UpdateAsync(user));
        Check(await userManager.UpdateSecurityStampAsync(user));
    }

    /// <summary>Role names per user, alphabetical.</summary>
    private async Task<Dictionary<Guid, IReadOnlyList<string>>> RolesOfAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        var pairs = await (from ur in db.UserRoles.AsNoTracking()
                           join r in db.Roles on ur.RoleId equals r.Id
                           where userIds.Contains(ur.UserId)
                           select new { ur.UserId, r.Name })
            .ToListAsync(cancellationToken);
        return pairs.GroupBy(p => p.UserId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(p => p.Name!).Order().ToList());
    }

    private async Task<AppUser> GetUserAsync(Guid userId) =>
        await userManager.FindByIdAsync(userId.ToString()) ?? throw AppException.NotFound("User not found.");

    private static void Check(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }
    }
}
