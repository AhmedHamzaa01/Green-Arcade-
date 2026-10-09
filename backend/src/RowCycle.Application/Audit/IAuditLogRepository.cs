using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Domain.Entities;

namespace RowCycle.Application.Audit;

/// <summary>Audit rows are only ever added.</summary>
public interface IAuditLogRepository
{
    void Add(AuditLog entry);

    /// <summary>Matching rows, newest first.</summary>
    Task<PagedResult<AuditLog>> SearchAsync(AuditLogQuery query, PageRequest page, CancellationToken cancellationToken = default);
}
