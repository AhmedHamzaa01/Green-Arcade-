using RowCycle.Domain.Common;

namespace RowCycle.Domain.Entities;

public class Address : ISoftDeletable
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>Who receives the parcel; the courier calls this phone.</summary>
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string Building { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsDefault { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}
