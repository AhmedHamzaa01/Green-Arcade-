namespace RowCycle.Domain.Entities;

/// <summary>Prices and rewards are snapshots taken at checkout (FR-15).</summary>
public class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid VariantId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPriceEgp { get; set; }
    public int? UnitPricePoints { get; set; }
    public int UnitRewardPoints { get; set; }
    public int Quantity { get; set; }
}
