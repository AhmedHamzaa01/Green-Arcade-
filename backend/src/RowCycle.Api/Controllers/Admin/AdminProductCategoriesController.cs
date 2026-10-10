using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RowCycle.Api.Authorization;
using RowCycle.Application.Catalog;
using RowCycle.Application.Dtos;

namespace RowCycle.Api.Controllers.Admin;

/// <summary>Manage store categories (StoreManager, Admin). Changes are audit-logged.</summary>
[ApiController]
[Route("admin/product-categories")]
[Authorize(Policy = Policies.StoreManager)]
[Produces("application/json")]
public sealed class AdminProductCategoriesController(ICatalogAdminService catalog) : ControllerBase
{
    /// <summary>All categories, including inactive ones.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProductCategoryResponse>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<ProductCategoryResponse>> List(CancellationToken cancellationToken) =>
        catalog.GetCategoriesAsync(cancellationToken);

    [HttpPost]
    [ProducesResponseType<ProductCategoryResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(SaveProductCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await catalog.CreateCategoryAsync(request, User.GetUserId(), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, category);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<ProductCategoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ProductCategoryResponse> Update(Guid id, SaveProductCategoryRequest request, CancellationToken cancellationToken) =>
        catalog.UpdateCategoryAsync(id, request, User.GetUserId(), cancellationToken);

    /// <summary>Soft delete. Refused (409) while the category still has products.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await catalog.DeleteCategoryAsync(id, User.GetUserId(), cancellationToken);
        return NoContent();
    }
}
