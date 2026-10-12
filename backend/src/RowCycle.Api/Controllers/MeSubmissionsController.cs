using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Application.Submissions;

namespace RowCycle.Api.Controllers;

/// <summary>The signed-in member's submissions and their status (F3).</summary>
[ApiController]
[Route("me/submissions")]
[Authorize]
[Produces("application/json")]
public sealed class MeSubmissionsController(ISubmissionService submissions) : ControllerBase
{
    /// <summary>Newest first.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<SubmissionResponse>>(StatusCodes.Status200OK)]
    public Task<PagedResult<SubmissionResponse>> List(
        CancellationToken cancellationToken, [FromQuery] int page = 1, [FromQuery] int pageSize = PageRequest.DefaultPageSize) =>
        submissions.ListMineAsync(User.GetUserId(), new PageRequest(page, pageSize), cancellationToken);
}
