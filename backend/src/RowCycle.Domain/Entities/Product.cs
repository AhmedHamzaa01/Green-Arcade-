using RowCycle.Domain.Enums;

namespace RowCycle.Domain.Entities;

public class Product
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public ProductCategory? Category { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal PriceEgp { get; set; }

    /// <summary>Null when the product can't be bought with points.</summary>
    public int? PricePoints { get; set; }

    /// <summary>Points the buyer earns per unit when the order is delivered (FR-18).</summary>
    public int RewardPoints { get; set; }

    public bool IsActive { get; set; } = true;
    public ProductType ProductType { get; set; } = ProductType.Consumer;

    /// <summary>Used for the "newest" sort (PRD F5).</summary>
    public DateTimeOffset CreatedAt { get; set; }

    public List<ProductVariant> Variants { get; set; } = [];
    public List<ProductImage> Images { get; set; } = [];
}
