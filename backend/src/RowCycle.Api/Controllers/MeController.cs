using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RowCycle.Application.Auth;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Application.Points;

namespace RowCycle.Api.Controllers;

/// <summary>The signed-in user's own data.</summary>
[ApiController]
[Route("me")]
[Authorize]
[Produces("application/json")]
public sealed class MeController(IAuthService auth, IPointsService points) : ControllerBase
{
    /// <summary>Profile, points balance and roles of the signed-in user.</summary>
    [HttpGet]
    [ProducesResponseType<MeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<MeResponse> Get(CancellationToken cancellationToken) =>
        auth.GetMeAsync(User.GetUserId(), cancellationToken);

    /// <summary>Points balance and history, newest first (F2).</summary>
    [HttpGet("points")]
    [ProducesResponseType<PointsHistoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<PointsHistoryResponse> GetPoints(
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize) =>
        points.GetHistoryAsync(User.GetUserId(), new PageRequest(page, pageSize), cancellationToken);
}
