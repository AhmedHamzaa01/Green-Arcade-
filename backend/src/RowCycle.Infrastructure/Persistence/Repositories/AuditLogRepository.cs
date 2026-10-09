using Microsoft.EntityFrameworkCore;
using RowCycle.Application.Audit;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Domain.Entities;

namespace RowCycle.Infrastructure.Persistence.Repositories;

internal sealed class AuditLogRepository(AppDbContext db) : IAuditLogRepository
{
    public void Add(AuditLog entry) => db.AuditLogs.Add(entry);

    public Task<PagedResult<AuditLog>> SearchAsync(AuditLogQuery query, PageRequest page, CancellationToken cancellationToken = default)
    {
        var logs = db.AuditLogs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Entity))
        {
            logs = logs.Where(l => l.Entity == query.Entity);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityId))
        {
            logs = logs.Where(l => l.EntityId == query.EntityId);
        }

        if (query.UserId is { } userId)
        {
            logs = logs.Where(l => l.UserId == userId);
        }

        return logs
            .OrderByDescending(l => l.CreatedAt)
            .ThenByDescending(l => l.Id)
            .ToPagedResultAsync(page, cancellationToken);
    }
}
