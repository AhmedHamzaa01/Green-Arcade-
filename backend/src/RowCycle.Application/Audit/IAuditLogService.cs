using RowCycle.Application.Common;
using RowCycle.Application.Dtos;

namespace RowCycle.Application.Audit;

/// <summary>Who changed what, and when (FR-19).</summary>
public interface IAuditLogService
{
    /// <summary>
    /// Adds an audit row. It's saved by the caller's unit of work, in the same transaction as the change,
    /// so a change and its audit row are saved together or not at all.
    /// </summary>
    void Record(Guid? userId, string action, string entity, string? entityId, object? data = null);

    Task<PagedResult<AuditLogResponse>> SearchAsync(AuditLogQuery query, PageRequest page, CancellationToken cancellationToken = default);
}
