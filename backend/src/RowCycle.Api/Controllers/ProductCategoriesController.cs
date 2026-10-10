using Microsoft.AspNetCore.Mvc;
using RowCycle.Application.Catalog;
using RowCycle.Application.Dtos;

namespace RowCycle.Api.Controllers;

/// <summary>Store categories (F5). Public.</summary>
[ApiController]
[Route("product-categories")]
[Produces("application/json")]
public sealed class ProductCategoriesController(ICatalogService catalog) : ControllerBase
{
    /// <summary>Active categories in display order.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProductCategoryResponse>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<ProductCategoryResponse>> List(CancellationToken cancellationToken) =>
        catalog.GetCategoriesAsync(cancellationToken);
}
