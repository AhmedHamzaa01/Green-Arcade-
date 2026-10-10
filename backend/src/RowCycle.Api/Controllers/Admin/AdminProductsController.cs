using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RowCycle.Api.Authorization;
using RowCycle.Application.Catalog;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;

namespace RowCycle.Api.Controllers.Admin;

/// <summary>Manage products, variants (stock) and images (StoreManager, Admin). Changes are audit-logged.</summary>
[ApiController]
[Route("admin/products")]
[Authorize(Policy = Policies.StoreManager)]
[Produces("application/json")]
public sealed class AdminProductsController(ICatalogAdminService catalog) : ControllerBase
{
    /// <summary>All products, including inactive ones, newest first.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<AdminProductListItem>>(StatusCodes.Status200OK)]
    public Task<PagedResult<AdminProductListItem>> Search(
        CancellationToken cancellationToken,
        [FromQuery] string? search = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize) =>
        catalog.SearchProductsAsync(new AdminProductQuery(search, categoryId, isActive), new PageRequest(page, pageSize), cancellationToken);

    [HttpGet("{id:guid}")]
    [ProducesResponseType<AdminProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<AdminProductResponse> Get(Guid id, CancellationToken cancellationToken) =>
        catalog.GetProductAsync(id, cancellationToken);

    /// <summary>Creates the product with a "Default" variant (stock 0).</summary>
    [HttpPost]
    [ProducesResponseType<AdminProductResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(SaveProductRequest request, CancellationToken cancellationToken)
    {
        var product = await catalog.CreateProductAsync(request, User.GetUserId(), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, product);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<AdminProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<AdminProductResponse> Update(Guid id, SaveProductRequest request, CancellationToken cancellationToken) =>
        catalog.UpdateProductAsync(id, request, User.GetUserId(), cancellationToken);

    /// <summary>Soft delete: hidden everywhere, but past orders keep their copy of it.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await catalog.DeleteProductAsync(id, User.GetUserId(), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/variants")]
    [ProducesResponseType<AdminProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<AdminProductResponse> AddVariant(Guid id, SaveProductVariantRequest request, CancellationToken cancellationToken) =>
        catalog.AddVariantAsync(id, request, User.GetUserId(), cancellationToken);

    /// <summary>Change a variant's name, SKU, stock or price.</summary>
    [HttpPut("{id:guid}/variants/{variantId:guid}")]
    [ProducesResponseType<AdminProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<AdminProductResponse> UpdateVariant(Guid id, Guid variantId, SaveProductVariantRequest request, CancellationToken cancellationToken) =>
        catalog.UpdateVariantAsync(id, variantId, request, User.GetUserId(), cancellationToken);

    /// <summary>Refused (409) for the last variant.</summary>
    [HttpDelete("{id:guid}/variants/{variantId:guid}")]
    [ProducesResponseType<AdminProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<AdminProductResponse> DeleteVariant(Guid id, Guid variantId, CancellationToken cancellationToken) =>
        catalog.DeleteVariantAsync(id, variantId, User.GetUserId(), cancellationToken);

    /// <summary>Upload one image (form field "file"): JPG, PNG or WEBP up to 10 MB.</summary>
    [HttpPost("{id:guid}/images")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 12 * 1024 * 1024)]
    [ProducesResponseType<AdminProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<AdminProductResponse> UploadImage(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        await using var content = file.OpenReadStream();
        return await catalog.UploadImageAsync(id, new FileUpload(content, file.FileName, file.Length), User.GetUserId(), cancellationToken);
    }

    [HttpDelete("{id:guid}/images/{imageId:guid}")]
    [ProducesResponseType<AdminProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<AdminProductResponse> DeleteImage(Guid id, Guid imageId, CancellationToken cancellationToken) =>
        catalog.DeleteImageAsync(id, imageId, User.GetUserId(), cancellationToken);

    /// <summary>Set the gallery order: send every image id once, first = main image.</summary>
    [HttpPut("{id:guid}/images/order")]
    [ProducesResponseType<AdminProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<AdminProductResponse> ReorderImages(Guid id, ReorderImagesRequest request, CancellationToken cancellationToken) =>
        catalog.ReorderImagesAsync(id, request, User.GetUserId(), cancellationToken);
}
