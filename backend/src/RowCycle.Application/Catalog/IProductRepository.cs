using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Domain.Entities;

namespace RowCycle.Application.Catalog;

/// <summary>Products with their category, variants and images. Soft-deleted rows are never returned.</summary>
public interface IProductRepository : IRepository<Product>
{
    /// <summary>Only active products in active categories (FR-13).</summary>
    Task<PagedResult<Product>> SearchPublicAsync(ProductQuery query, PageRequest page, CancellationToken cancellationToken = default);

    /// <summary>Only an active product in an active category.</summary>
    Task<Product?> FindPublicBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>Every product, active or not, newest first.</summary>
    Task<PagedResult<Product>> SearchAdminAsync(AdminProductQuery query, PageRequest page, CancellationToken cancellationToken = default);

    /// <summary>Tracked, with category, variants and images, for editing.</summary>
    Task<Product?> FindForEditAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(string slug, Guid? exceptProductId = null, CancellationToken cancellationToken = default);

    Task<bool> SkuExistsAsync(string sku, Guid? exceptVariantId = null, CancellationToken cancellationToken = default);

    /// <summary>Adds a new variant to a loaded product (inserted on save).</summary>
    void AddVariant(Product product, ProductVariant variant);

    /// <summary>Adds a new image to a loaded product (inserted on save).</summary>
    void AddImage(Product product, ProductImage image);

    /// <summary>Soft-deletes the variant.</summary>
    void RemoveVariant(ProductVariant variant);

    /// <summary>Soft-deletes the image row (the file is kept).</summary>
    void RemoveImage(ProductImage image);
}
