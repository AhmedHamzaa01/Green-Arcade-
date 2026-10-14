using RowCycle.Application.Dtos;

namespace RowCycle.Application.Badges;

/// <summary>Badges (F4): members see what they earned; admins define the rules. Admin changes are audit-logged.</summary>
public interface IBadgeService
{
    Task<IReadOnlyList<MyBadgeResponse>> ListMineAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BadgeResponse>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>The icon names a badge can use.</summary>
    IReadOnlyList<string> GetIcons();

    /// <summary>Applies from the members' next approval or delivery; members who already qualify aren't backfilled.</summary>
    Task<BadgeResponse> CreateAsync(SaveBadgeRequest request, Guid adminId, CancellationToken cancellationToken = default);

    /// <summary>Members who already earned the badge keep it.</summary>
    Task<BadgeResponse> UpdateAsync(Guid badgeId, SaveBadgeRequest request, Guid adminId, CancellationToken cancellationToken = default);

    /// <summary>Soft delete: no new awards; members who earned it keep it.</summary>
    Task DeleteAsync(Guid badgeId, Guid adminId, CancellationToken cancellationToken = default);
}
