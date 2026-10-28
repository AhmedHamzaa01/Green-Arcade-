using RowCycle.Application.Common;
using RowCycle.Application.Dtos;

namespace RowCycle.Application.Users;

/// <summary>
/// Users across the login account (Identity), profile and roles, for the admin area (F8).
/// Implemented in Infrastructure because it works on the Identity tables.
/// </summary>
public interface IUserDirectory
{
    /// <summary>Newest first.</summary>
    Task<PagedResult<AdminUserListItem>> SearchAsync(UserQuery query, PageRequest page, CancellationToken cancellationToken = default);

    Task<AdminUserDetail?> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the user's roles with <paramref name="roles"/> and changes their security stamp, so their current
    /// access token stops working and the next one carries the new roles.
    /// </summary>
    Task SetRolesAsync(Guid userId, IReadOnlyCollection<string> roles, CancellationToken cancellationToken = default);

    /// <summary>Blocks (or unblocks) login and changes the security stamp, so a blocked user's tokens stop working at once.</summary>
    Task SetActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default);
}
