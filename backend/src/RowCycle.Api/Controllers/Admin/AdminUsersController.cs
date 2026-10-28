using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RowCycle.Api.Authorization;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Application.Users;

namespace RowCycle.Api.Controllers.Admin;

/// <summary>Manage users (Admin): roles, blocking, manual points. Changes are audit-logged (FR-19).</summary>
[ApiController]
[Route("admin/users")]
[Authorize(Policy = Policies.Admin)]
[Produces("application/json")]
public sealed class AdminUsersController(IUserAdminService users) : ControllerBase
{
    /// <summary>Newest first. Search matches name or email; filter by role (e.g. Moderator) or blocked.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<AdminUserListItem>>(StatusCodes.Status200OK)]
    public Task<PagedResult<AdminUserListItem>> Search(
        CancellationToken cancellationToken,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        [FromQuery] bool? blocked = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize) =>
        users.SearchAsync(new UserQuery(search, role, blocked), new PageRequest(page, pageSize), cancellationToken);

    [HttpGet("{id:guid}")]
    [ProducesResponseType<AdminUserDetail>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<AdminUserDetail> Get(Guid id, CancellationToken cancellationToken) => users.GetAsync(id, cancellationToken);

    /// <summary>The user's balance and points history, newest first.</summary>
    [HttpGet("{id:guid}/points")]
    [ProducesResponseType<PointsHistoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<PointsHistoryResponse> GetPoints(
        Guid id, CancellationToken cancellationToken, [FromQuery] int page = 1, [FromQuery] int pageSize = PageRequest.DefaultPageSize) =>
        users.GetPointsAsync(id, new PageRequest(page, pageSize), cancellationToken);

    /// <summary>Replaces the user's roles (any combination, at least one). Takes effect on their next request.</summary>
    [HttpPut("{id:guid}/roles")]
    [ProducesResponseType<AdminUserDetail>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<AdminUserDetail> SetRoles(Guid id, SetRolesRequest request, CancellationToken cancellationToken) =>
        users.SetRolesAsync(id, request, User.GetUserId(), cancellationToken);

    /// <summary><c>{ "blocked": true }</c> ends all their sessions at once and stops login; <c>false</c> lets them log in again.</summary>
    [HttpPut("{id:guid}/block")]
    [ProducesResponseType<AdminUserDetail>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<AdminUserDetail> SetBlocked(Guid id, SetBlockedRequest request, CancellationToken cancellationToken) =>
        users.SetBlockedAsync(id, request, User.GetUserId(), cancellationToken);

    /// <summary>Adds (positive) or removes (negative) points with a reason (FR-07). The balance can't go below 0.</summary>
    [HttpPost("{id:guid}/points-adjustments")]
    [ProducesResponseType<PointsChange>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AdjustPoints(Guid id, AdjustPointsRequest request, CancellationToken cancellationToken)
    {
        var change = await users.AdjustPointsAsync(id, request, User.GetUserId(), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, change);
    }
}
