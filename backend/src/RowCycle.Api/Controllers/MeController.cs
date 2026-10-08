using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RowCycle.Application.Auth;

namespace RowCycle.Api.Controllers;

/// <summary>The signed-in user's own data.</summary>
[ApiController]
[Route("me")]
[Authorize]
[Produces("application/json")]
public sealed class MeController(IAuthService auth) : ControllerBase
{
    /// <summary>Profile, points balance and roles of the signed-in user.</summary>
    [HttpGet]
    [ProducesResponseType<MeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<MeResponse> Get(CancellationToken cancellationToken) =>
        auth.GetMeAsync(User.GetUserId(), cancellationToken);
}
