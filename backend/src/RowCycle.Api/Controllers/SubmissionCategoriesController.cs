using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RowCycle.Application.Dtos;
using RowCycle.Application.Submissions;

namespace RowCycle.Api.Controllers;

/// <summary>Actions members can submit, with their points (F3).</summary>
[ApiController]
[Route("submission-categories")]
[Authorize]
[Produces("application/json")]
public sealed class SubmissionCategoriesController(ISubmissionService submissions) : ControllerBase
{
    /// <summary>Active categories, by name. <c>tracksWeight</c> says whether the form asks for a weight.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<SubmissionCategoryResponse>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<SubmissionCategoryResponse>> List(CancellationToken cancellationToken) =>
        submissions.GetCategoriesAsync(cancellationToken);
}
