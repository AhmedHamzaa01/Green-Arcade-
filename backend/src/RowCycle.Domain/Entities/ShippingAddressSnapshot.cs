namespace RowCycle.Domain.Entities;

/// <summary>Copy of the delivery address taken at checkout, stored as jsonb on the order.</summary>
public class ShippingAddressSnapshot
{
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string City { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string Building { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
