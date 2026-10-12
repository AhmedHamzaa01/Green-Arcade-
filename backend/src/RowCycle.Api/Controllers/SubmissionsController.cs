using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RowCycle.Api.Authorization;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Application.Submissions;

namespace RowCycle.Api.Controllers;

/// <summary>Submit a photo-verified action (F3). Needs a confirmed email (FR-02).</summary>
[ApiController]
[Route("submissions")]
[Authorize(Policy = Policies.VerifiedMember)]
[Produces("application/json")]
public sealed class SubmissionsController(ISubmissionService submissions) : ControllerBase
{
    /// <summary>
    /// Multipart form: <c>photo</c> (JPG, PNG or WEBP up to 10 MB), <c>categoryId</c>, optional <c>note</c>,
    /// and <c>weightKg</c> only for categories that track weight. Waits for a moderator as Pending.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 12 * 1024 * 1024)]
    [ProducesResponseType<SubmissionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromForm] CreateSubmissionForm form, CancellationToken cancellationToken)
    {
        await using var content = form.Photo.OpenReadStream();
        var submission = await submissions.CreateAsync(
            User.GetUserId(),
            new CreateSubmissionRequest(form.CategoryId, form.Note, form.WeightKg),
            new FileUpload(content, form.Photo.FileName, form.Photo.Length),
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, submission);
    }
}

/// <summary>The multipart fields of <c>POST /submissions</c>.</summary>
public sealed class CreateSubmissionForm
{
    public required IFormFile Photo { get; init; }
    public Guid CategoryId { get; init; }
    public string? Note { get; init; }
    public decimal? WeightKg { get; init; }
}
