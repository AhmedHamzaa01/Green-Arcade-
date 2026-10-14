using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RowCycle.Api.Authorization;
using RowCycle.Application.Dtos;
using RowCycle.Application.Submissions;

namespace RowCycle.Api.Controllers.Admin;

/// <summary>The actions members can submit and their points (Admin). Changes are audit-logged.</summary>
[ApiController]
[Route("admin/submission-categories")]
[Authorize(Policy = Policies.Admin)]
[Produces("application/json")]
public sealed class AdminSubmissionCategoriesController(ISubmissionCategoryAdminService categories) : ControllerBase
{
    /// <summary>All categories, including inactive ones.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AdminSubmissionCategoryResponse>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<AdminSubmissionCategoryResponse>> List(CancellationToken cancellationToken) =>
        categories.ListAsync(cancellationToken);

    [HttpPost]
    [ProducesResponseType<AdminSubmissionCategoryResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(SaveSubmissionCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await categories.CreateAsync(request, User.GetUserId(), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, category);
    }

    /// <summary>New points apply to future approvals only.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<AdminSubmissionCategoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<AdminSubmissionCategoryResponse> Update(Guid id, SaveSubmissionCategoryRequest request, CancellationToken cancellationToken) =>
        categories.UpdateAsync(id, request, User.GetUserId(), cancellationToken);

    /// <summary>Soft delete. Refused (409) while a badge counts this category; deactivate it instead.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await categories.DeleteAsync(id, User.GetUserId(), cancellationToken);
        return NoContent();
    }
}
