using System.Text.Json;

namespace RowCycle.Application.Dtos;

public sealed record AuditLogResponse(
    Guid Id,
    Guid? UserId,
    string Action,
    string Entity,
    string? EntityId,
    JsonElement? Data,
    DateTimeOffset CreatedAt);

/// <summary>Filters for <c>GET /admin/audit-logs</c>. All optional.</summary>
public sealed record AuditLogQuery(string? Entity = null, string? EntityId = null, Guid? UserId = null);
