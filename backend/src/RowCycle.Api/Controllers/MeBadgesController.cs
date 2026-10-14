using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RowCycle.Application.Badges;
using RowCycle.Application.Dtos;

namespace RowCycle.Api.Controllers;

/// <summary>The signed-in member's badges (F4).</summary>
[ApiController]
[Route("me/badges")]
[Authorize]
[Produces("application/json")]
public sealed class MeBadgesController(IBadgeService badges) : ControllerBase
{
    /// <summary>Newest first. A badge stays here even if an admin deletes it later.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<MyBadgeResponse>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<MyBadgeResponse>> List(CancellationToken cancellationToken) =>
        badges.ListMineAsync(User.GetUserId(), cancellationToken);
}
