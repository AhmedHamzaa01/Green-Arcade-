using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RowCycle.Api.Authorization;
using RowCycle.Application.Audit;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;

namespace RowCycle.Api.Controllers.Admin;

/// <summary>Read the audit log: who changed what, and when (FR-19).</summary>
[ApiController]
[Route("admin/audit-logs")]
[Authorize(Policy = Policies.Admin)]
[Produces("application/json")]
public sealed class AdminAuditLogsController(IAuditLogService audit) : ControllerBase
{
    /// <summary>Newest first. Filter by entity (e.g. "settings"), entity id, or the user who made the change.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<AuditLogResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<PagedResult<AuditLogResponse>> Search(
        CancellationToken cancellationToken,
        [FromQuery] string? entity = null,
        [FromQuery] string? entityId = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize) =>
        audit.SearchAsync(new AuditLogQuery(entity, entityId, userId), new PageRequest(page, pageSize), cancellationToken);
}
