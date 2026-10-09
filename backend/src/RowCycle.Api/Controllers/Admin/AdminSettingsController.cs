using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RowCycle.Api.Authorization;
using RowCycle.Application.Dtos;
using RowCycle.Application.Settings;

namespace RowCycle.Api.Controllers.Admin;

/// <summary>Admin-editable settings (PRD F8). Every change is audit-logged (FR-19).</summary>
[ApiController]
[Route("admin/settings")]
[Authorize(Policy = Policies.Admin)]
[Produces("application/json")]
public sealed class AdminSettingsController(ISettingsService settings) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<SettingsResponse>(StatusCodes.Status200OK)]
    public Task<SettingsResponse> Get(CancellationToken cancellationToken) => settings.GetAsync(cancellationToken);

    /// <summary>Change settings. Returns the saved values.</summary>
    [HttpPut]
    [ProducesResponseType<SettingsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<SettingsResponse> Update(UpdateSettingsRequest request, CancellationToken cancellationToken) =>
        settings.UpdateAsync(request, User.GetUserId(), cancellationToken);
}
