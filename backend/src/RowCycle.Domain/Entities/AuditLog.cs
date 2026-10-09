using RowCycle.Domain.Common;

namespace RowCycle.Domain.Entities;

/// <summary>One row per admin write (FR-19).</summary>
public class AuditLog : IAppendOnly
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;

    /// <summary>Text so it can hold a uuid or a settings key.</summary>
    public string? EntityId { get; set; }

    /// <summary>JSON text (stored as jsonb).</summary>
    public string? Data { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
