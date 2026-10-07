namespace RowCycle.Domain.Entities;

public class CartItem
{
    public Guid CartId { get; set; }
    public Guid VariantId { get; set; }
    public ProductVariant? Variant { get; set; }
    public int Quantity { get; set; }
}
