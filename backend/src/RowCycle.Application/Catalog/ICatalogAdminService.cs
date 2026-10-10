using RowCycle.Application.Common;
using RowCycle.Application.Dtos;

namespace RowCycle.Application.Catalog;

/// <summary>
/// Catalog management for store managers (PRD F8). Every change is audit-logged in the same transaction (FR-19).
/// Deletes are soft deletes.
/// </summary>
public interface ICatalogAdminService
{
    // Categories
    Task<IReadOnlyList<ProductCategoryResponse>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<ProductCategoryResponse> CreateCategoryAsync(SaveProductCategoryRequest request, Guid staffId, CancellationToken cancellationToken = default);
    Task<ProductCategoryResponse> UpdateCategoryAsync(Guid id, SaveProductCategoryRequest request, Guid staffId, CancellationToken cancellationToken = default);

    /// <summary>Refused while products are still in the category: move or delete them first.</summary>
    Task DeleteCategoryAsync(Guid id, Guid staffId, CancellationToken cancellationToken = default);

    // Products
    Task<PagedResult<AdminProductListItem>> SearchProductsAsync(AdminProductQuery query, PageRequest page, CancellationToken cancellationToken = default);
    Task<AdminProductResponse> GetProductAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Creates the product with one "Default" variant (stock 0), so it can be sold once stock is set.</summary>
    Task<AdminProductResponse> CreateProductAsync(SaveProductRequest request, Guid staffId, CancellationToken cancellationToken = default);
    Task<AdminProductResponse> UpdateProductAsync(Guid id, SaveProductRequest request, Guid staffId, CancellationToken cancellationToken = default);
    Task DeleteProductAsync(Guid id, Guid staffId, CancellationToken cancellationToken = default);

    // Variants (stock lives here)
    Task<AdminProductResponse> AddVariantAsync(Guid productId, SaveProductVariantRequest request, Guid staffId, CancellationToken cancellationToken = default);
    Task<AdminProductResponse> UpdateVariantAsync(Guid productId, Guid variantId, SaveProductVariantRequest request, Guid staffId, CancellationToken cancellationToken = default);

    /// <summary>Refused for the last variant: every product keeps at least one.</summary>
    Task<AdminProductResponse> DeleteVariantAsync(Guid productId, Guid variantId, Guid staffId, CancellationToken cancellationToken = default);

    // Images
    /// <summary>JPG, PNG or WEBP up to 10 MB, checked by content (NFR-04). Added at the end of the gallery.</summary>
    Task<AdminProductResponse> UploadImageAsync(Guid productId, FileUpload file, Guid staffId, CancellationToken cancellationToken = default);
    Task<AdminProductResponse> DeleteImageAsync(Guid productId, Guid imageId, Guid staffId, CancellationToken cancellationToken = default);
    Task<AdminProductResponse> ReorderImagesAsync(Guid productId, ReorderImagesRequest request, Guid staffId, CancellationToken cancellationToken = default);
}
