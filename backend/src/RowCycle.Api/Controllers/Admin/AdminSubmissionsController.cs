using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RowCycle.Api.Authorization;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Application.Submissions;
using RowCycle.Domain.Enums;

namespace RowCycle.Api.Controllers.Admin;

/// <summary>Review submissions (Moderator, Admin). Reviews are audit-logged.</summary>
[ApiController]
[Route("admin/submissions")]
[Authorize(Policy = Policies.Moderator)]
[Produces("application/json")]
public sealed class AdminSubmissionsController(ISubmissionModerationService moderation) : ControllerBase
{
    /// <summary>Oldest first, so the longest-waiting are reviewed first. Filter by status (e.g. Pending).</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<SubmissionResponse>>(StatusCodes.Status200OK)]
    public Task<PagedResult<SubmissionResponse>> Search(
        CancellationToken cancellationToken,
        [FromQuery] SubmissionStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize) =>
        moderation.SearchAsync(new SubmissionQuery(status), new PageRequest(page, pageSize), cancellationToken);

    [HttpGet("{id:guid}")]
    [ProducesResponseType<SubmissionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<SubmissionResponse> Get(Guid id, CancellationToken cancellationToken) => moderation.GetAsync(id, cancellationToken);

    /// <summary>Gives the category's points to the member. Only a Pending submission; a second approve gets 409.</summary>
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType<SubmissionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<SubmissionResponse> Approve(Guid id, CancellationToken cancellationToken) =>
        moderation.ApproveAsync(id, User.GetUserId(), cancellationToken);

    /// <summary>A reason is required; the member sees it. No points.</summary>
    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType<SubmissionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<SubmissionResponse> Reject(Guid id, RejectSubmissionRequest request, CancellationToken cancellationToken) =>
        moderation.RejectAsync(id, request, User.GetUserId(), cancellationToken);
}
