using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RowCycle.Api.Authorization;
using RowCycle.Application.Dashboard;
using RowCycle.Application.Dtos;

namespace RowCycle.Api.Controllers.Admin;

/// <summary>The staff home screen (Moderator, StoreManager, Admin).</summary>
[ApiController]
[Route("admin/dashboard")]
[Authorize(Policy = Policies.Staff)]
[Produces("application/json")]
public sealed class AdminDashboardController(IDashboardService dashboard) : ControllerBase
{
    /// <summary>Pending submissions, orders waiting to be confirmed, and variants at or below the low-stock setting.</summary>
    [HttpGet]
    [ProducesResponseType<DashboardResponse>(StatusCodes.Status200OK)]
    public Task<DashboardResponse> Get(CancellationToken cancellationToken) => dashboard.GetAsync(cancellationToken);
}
