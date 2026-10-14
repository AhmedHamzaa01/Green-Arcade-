using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RowCycle.Api.Authorization;
using RowCycle.Application.Badges;
using RowCycle.Application.Dtos;

namespace RowCycle.Api.Controllers.Admin;

/// <summary>Define badges and their rules (Admin). Changes are audit-logged.</summary>
[ApiController]
[Route("admin/badges")]
[Authorize(Policy = Policies.Admin)]
[Produces("application/json")]
public sealed class AdminBadgesController(IBadgeService badges) : ControllerBase
{
    /// <summary>Badges that aren't deleted, by name.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<BadgeResponse>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<BadgeResponse>> List(CancellationToken cancellationToken) => badges.ListAsync(cancellationToken);

    /// <summary>The icon names a badge can use (Material Symbols).</summary>
    [HttpGet("icons")]
    [ProducesResponseType<IReadOnlyList<string>>(StatusCodes.Status200OK)]
    public IReadOnlyList<string> Icons() => badges.GetIcons();

    /// <summary>
    /// Members get it at their next approval or delivery that meets the rule.
    /// <c>ruleCategoryId</c> only for <c>CategoryApproved</c>; <c>threshold</c> is ignored for <c>FirstOrder</c>.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<BadgeResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(SaveBadgeRequest request, CancellationToken cancellationToken)
    {
        var badge = await badges.CreateAsync(request, User.GetUserId(), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, badge);
    }

    /// <summary>Members who already earned the badge keep it.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<BadgeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<BadgeResponse> Update(Guid id, SaveBadgeRequest request, CancellationToken cancellationToken) =>
        badges.UpdateAsync(id, request, User.GetUserId(), cancellationToken);

    /// <summary>Soft delete: no new awards; members who earned it keep it.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await badges.DeleteAsync(id, User.GetUserId(), cancellationToken);
        return NoContent();
    }
}
