using System.Text.Json;
using System.Text.Json.Serialization;
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
    /// <summary>camelCase, with enums as words ("Confirmed", not 1), so the log reads well.</summary>
    private static readonly JsonSerializerOptions DataJson = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public void Record(Guid? userId, string action, string entity, string? entityId, object? data = null) =>
        auditLogs.Add(new AuditLog
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            Data = data is null ? null : JsonSerializer.Serialize(data, DataJson),
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
