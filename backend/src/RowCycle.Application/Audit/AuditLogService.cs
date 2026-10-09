using System.Text.Json;
using AutoMapper;
using FluentValidation;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Domain.Entities;

namespace RowCycle.Application.Audit;

internal sealed class AuditLogService(
    IAuditLogRepository auditLogs,
    IValidator<PageRequest> pageValidator,
    IMapper mapper,
    TimeProvider timeProvider) : IAuditLogService
{
    public void Record(Guid? userId, string action, string entity, string? entityId, object? data = null) =>
        auditLogs.Add(new AuditLog
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            Data = data is null ? null : JsonSerializer.Serialize(data, JsonSerializerOptions.Web),
            CreatedAt = timeProvider.GetUtcNow(),
        });

    public async Task<PagedResult<AuditLogResponse>> SearchAsync(
        AuditLogQuery query, PageRequest page, CancellationToken cancellationToken = default)
    {
        await pageValidator.ValidateAndThrowAsync(page, cancellationToken);
        var result = await auditLogs.SearchAsync(query, page, cancellationToken);
        return result.Map(mapper.Map<AuditLogResponse>);
    }
}
