using AutoMapper;
using FluentValidation;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;

namespace RowCycle.Application.Catalog;

internal sealed class CatalogService(
    IProductCategoryRepository categories,
    IProductRepository products,
    IValidator<ProductQuery> queryValidator,
    IValidator<PageRequest> pageValidator,
    IMapper mapper) : ICatalogService
{
    public async Task<IReadOnlyList<ProductCategoryResponse>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        mapper.Map<List<ProductCategoryResponse>>(await categories.ListAsync(activeOnly: true, cancellationToken));

    public async Task<PagedResult<ProductListItem>> SearchProductsAsync(
        ProductQuery query, PageRequest page, CancellationToken cancellationToken = default)
    {
        await queryValidator.ValidateAndThrowAsync(query, cancellationToken);
        await pageValidator.ValidateAndThrowAsync(page, cancellationToken);

        var result = await products.SearchPublicAsync(query, page, cancellationToken);
        return result.Map(mapper.Map<ProductListItem>);
    }

    public async Task<ProductDetailResponse> GetProductAsync(string slug, CancellationToken cancellationToken = default)
    {
        // Inactive and deleted products answer 404 here, exactly like products that never existed.
        var product = await products.FindPublicBySlugAsync(slug, cancellationToken)
            ?? throw AppException.NotFound("Product not found.");
        return mapper.Map<ProductDetailResponse>(product);
    }
}
