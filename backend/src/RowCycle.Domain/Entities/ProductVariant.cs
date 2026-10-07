namespace RowCycle.Domain.Entities;

/// <summary>Every product has at least one variant; stock lives here.</summary>
public class ProductVariant
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public int Stock { get; set; }

    /// <summary>EGP price for this variant when it differs from <see cref="Product.PriceEgp"/>.</summary>
    public decimal? PriceOverride { get; set; }
}
