using RowCycle.Application.Common;
using RowCycle.Application.Dtos;

namespace RowCycle.Application.Catalog;

/// <summary>What shoppers can see (F5, FR-13): only active products in active categories.</summary>
public interface ICatalogService
{
    Task<IReadOnlyList<ProductCategoryResponse>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<ProductListItem>> SearchProductsAsync(ProductQuery query, PageRequest page, CancellationToken cancellationToken = default);

    Task<ProductDetailResponse> GetProductAsync(string slug, CancellationToken cancellationToken = default);
}
