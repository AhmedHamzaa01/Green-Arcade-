using Microsoft.AspNetCore.Mvc;
using RowCycle.Application.Catalog;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;

namespace RowCycle.Api.Controllers;

/// <summary>The store catalog (F5, FR-13). Public; only active products in active categories.</summary>
[ApiController]
[Route("products")]
[Produces("application/json")]
public sealed class ProductsController(ICatalogService catalog) : ControllerBase
{
    /// <summary>Search, filter by category slug, sort (Newest, PriceAsc, PriceDesc), paged.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<ProductListItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<PagedResult<ProductListItem>> Search(
        CancellationToken cancellationToken,
        [FromQuery] string? category = null,
        [FromQuery] string? search = null,
        [FromQuery] ProductSort sort = ProductSort.Newest,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize) =>
        catalog.SearchProductsAsync(new ProductQuery(category, search, sort), new PageRequest(page, pageSize), cancellationToken);

    [HttpGet("{slug}")]
    [ProducesResponseType<ProductDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ProductDetailResponse> Get(string slug, CancellationToken cancellationToken) =>
        catalog.GetProductAsync(slug, cancellationToken);
}
