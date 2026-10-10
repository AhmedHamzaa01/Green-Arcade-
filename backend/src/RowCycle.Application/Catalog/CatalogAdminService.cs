using AutoMapper;
using FluentValidation;
using RowCycle.Application.Audit;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace RowCycle.Application.Catalog;

internal sealed class CatalogAdminService(
    IProductCategoryRepository categories,
    IProductRepository products,
    IFileStorage fileStorage,
    IAuditLogService audit,
    IUnitOfWork unitOfWork,
    IServiceProvider services,
    IMapper mapper,
    TimeProvider timeProvider) : ICatalogAdminService
{
    private const string ImageFolder = "products";

    // ---------- Categories ----------

    public async Task<IReadOnlyList<ProductCategoryResponse>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        mapper.Map<List<ProductCategoryResponse>>(await categories.ListAsync(activeOnly: false, cancellationToken));

    public async Task<ProductCategoryResponse> CreateCategoryAsync(
        SaveProductCategoryRequest request, Guid staffId, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);

        var category = new ProductCategory { Id = Guid.CreateVersion7() };
        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await ApplyAsync(category, request, ct);
            categories.Add(category);
            audit.Record(staffId, AuditActions.CategoryCreate, "product_category", category.Id.ToString(), request);
        }, cancellationToken);

        return mapper.Map<ProductCategoryResponse>(category);
    }

    public async Task<ProductCategoryResponse> UpdateCategoryAsync(
        Guid id, SaveProductCategoryRequest request, Guid staffId, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);

        var category = await FindCategoryAsync(id, cancellationToken);
        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var before = mapper.Map<ProductCategoryResponse>(category);
            await ApplyAsync(category, request, ct);
            audit.Record(staffId, AuditActions.CategoryUpdate, "product_category", id.ToString(),
                new { before, after = mapper.Map<ProductCategoryResponse>(category) });
        }, cancellationToken);

        return mapper.Map<ProductCategoryResponse>(category);
    }

    public async Task DeleteCategoryAsync(Guid id, Guid staffId, CancellationToken cancellationToken = default)
    {
        var category = await FindCategoryAsync(id, cancellationToken);
        if (await categories.HasProductsAsync(id, cancellationToken))
        {
            throw AppException.Conflict("Category is not empty.", "Move or delete its products first, or deactivate the category.");
        }

        await unitOfWork.ExecuteInTransactionAsync(_ =>
        {
            categories.Remove(category);
            audit.Record(staffId, AuditActions.CategoryDelete, "product_category", id.ToString(), new { category.Name, category.Slug });
            return Task.CompletedTask;
        }, cancellationToken);
    }

    // ---------- Products ----------

    public async Task<PagedResult<AdminProductListItem>> SearchProductsAsync(
        AdminProductQuery query, PageRequest page, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(query, cancellationToken);
        await ValidateAsync(page, cancellationToken);
        return (await products.SearchAdminAsync(query, page, cancellationToken)).Map(mapper.Map<AdminProductListItem>);
    }

    public async Task<AdminProductResponse> GetProductAsync(Guid id, CancellationToken cancellationToken = default) =>
        mapper.Map<AdminProductResponse>(await FindProductAsync(id, cancellationToken));

    public async Task<AdminProductResponse> CreateProductAsync(
        SaveProductRequest request, Guid staffId, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);

        var product = new Product { Id = Guid.CreateVersion7(), CreatedAt = timeProvider.GetUtcNow() };
        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await ApplyAsync(product, request, ct);

            // Stock lives on variants, so every product starts with one.
            var sku = await Slug.MakeUniqueAsync(Truncate($"{product.Slug}-default", 60), s => products.SkuExistsAsync(s, null, ct));
            product.Variants.Add(new ProductVariant { Id = Guid.CreateVersion7(), Name = "Default", Sku = sku, Stock = 0 });

            products.Add(product);
            audit.Record(staffId, AuditActions.ProductCreate, "product", product.Id.ToString(), request);
        }, cancellationToken);

        return await GetProductAsync(product.Id, cancellationToken);
    }

    public async Task<AdminProductResponse> UpdateProductAsync(
        Guid id, SaveProductRequest request, Guid staffId, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);

        var product = await FindProductAsync(id, cancellationToken);
        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var before = Snapshot(product);
            await ApplyAsync(product, request, ct);
            audit.Record(staffId, AuditActions.ProductUpdate, "product", id.ToString(), new { before, after = Snapshot(product) });
        }, cancellationToken);

        return mapper.Map<AdminProductResponse>(product);
    }

    public async Task DeleteProductAsync(Guid id, Guid staffId, CancellationToken cancellationToken = default)
    {
        var product = await FindProductAsync(id, cancellationToken);
        await unitOfWork.ExecuteInTransactionAsync(_ =>
        {
            // Soft delete: past orders keep pointing at it; it disappears from the store and admin lists.
            products.Remove(product);
            audit.Record(staffId, AuditActions.ProductDelete, "product", id.ToString(), new { product.Name, product.Slug });
            return Task.CompletedTask;
        }, cancellationToken);
    }

    // ---------- Variants ----------

    public async Task<AdminProductResponse> AddVariantAsync(
        Guid productId, SaveProductVariantRequest request, Guid staffId, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);

        var product = await FindProductAsync(productId, cancellationToken);
        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await EnsureSkuIsFreeAsync(request.Sku, null, ct);
            var variant = new ProductVariant { Id = Guid.CreateVersion7(), ProductId = productId };
            Apply(variant, request);
            products.AddVariant(product, variant);
            audit.Record(staffId, AuditActions.VariantCreate, "product_variant", variant.Id.ToString(), new { productId, request });
        }, cancellationToken);

        return mapper.Map<AdminProductResponse>(product);
    }

    public async Task<AdminProductResponse> UpdateVariantAsync(
        Guid productId, Guid variantId, SaveProductVariantRequest request, Guid staffId, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);

        var product = await FindProductAsync(productId, cancellationToken);
        var variant = FindVariant(product, variantId);
        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await EnsureSkuIsFreeAsync(request.Sku, variantId, ct);
            var before = mapper.Map<AdminProductVariantResponse>(variant);
            Apply(variant, request);
            // Stock changes are logged here with old and new values.
            audit.Record(staffId, AuditActions.VariantUpdate, "product_variant", variantId.ToString(),
                new { productId, before, after = mapper.Map<AdminProductVariantResponse>(variant) });
        }, cancellationToken);

        return mapper.Map<AdminProductResponse>(product);
    }

    public async Task<AdminProductResponse> DeleteVariantAsync(
        Guid productId, Guid variantId, Guid staffId, CancellationToken cancellationToken = default)
    {
        var product = await FindProductAsync(productId, cancellationToken);
        var variant = FindVariant(product, variantId);
        if (product.Variants.Count == 1)
        {
            throw AppException.Conflict("A product needs at least one variant.", "Add another variant before deleting this one.");
        }

        await unitOfWork.ExecuteInTransactionAsync(_ =>
        {
            products.RemoveVariant(variant);
            product.Variants.Remove(variant);
            audit.Record(staffId, AuditActions.VariantDelete, "product_variant", variantId.ToString(), new { productId, variant.Name, variant.Sku });
            return Task.CompletedTask;
        }, cancellationToken);

        return mapper.Map<AdminProductResponse>(product);
    }

    // ---------- Images ----------

    public async Task<AdminProductResponse> UploadImageAsync(
        Guid productId, FileUpload file, Guid staffId, CancellationToken cancellationToken = default)
    {
        var product = await FindProductAsync(productId, cancellationToken);

        if (file.Length is <= 0 or > ImageFileCheck.MaxBytes)
        {
            throw AppException.Validation("Image is too large.", "Use a file up to 10 MB.");
        }

        var extension = await ImageFileCheck.DetectExtensionAsync(file.Content, cancellationToken)
            ?? throw AppException.Validation("Unsupported image.", "Use a JPG, PNG or WEBP image.");

        var url = await fileStorage.SaveAsync(file.Content, ImageFolder, extension, cancellationToken);
        try
        {
            await unitOfWork.ExecuteInTransactionAsync(_ =>
            {
                var image = new ProductImage
                {
                    Id = Guid.CreateVersion7(),
                    ProductId = productId,
                    Url = url,
                    SortOrder = product.Images.Count == 0 ? 0 : product.Images.Max(i => i.SortOrder) + 1,
                };
                products.AddImage(product, image);
                audit.Record(staffId, AuditActions.ImageUpload, "product_image", image.Id.ToString(), new { productId, url, file.FileName });
                return Task.CompletedTask;
            }, cancellationToken);
        }
        catch
        {
            // Don't leave a file nobody points to.
            await fileStorage.DeleteAsync(url, CancellationToken.None);
            throw;
        }

        return mapper.Map<AdminProductResponse>(product);
    }

    public async Task<AdminProductResponse> DeleteImageAsync(
        Guid productId, Guid imageId, Guid staffId, CancellationToken cancellationToken = default)
    {
        var product = await FindProductAsync(productId, cancellationToken);
        var image = product.Images.SingleOrDefault(i => i.Id == imageId) ?? throw AppException.NotFound("Image not found.");

        await unitOfWork.ExecuteInTransactionAsync(_ =>
        {
            products.RemoveImage(image);
            product.Images.Remove(image);
            audit.Record(staffId, AuditActions.ImageDelete, "product_image", imageId.ToString(), new { productId, image.Url });
            return Task.CompletedTask;
        }, cancellationToken);

        return mapper.Map<AdminProductResponse>(product);
    }

    public async Task<AdminProductResponse> ReorderImagesAsync(
        Guid productId, ReorderImagesRequest request, Guid staffId, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);

        var product = await FindProductAsync(productId, cancellationToken);
        var current = product.Images.Select(i => i.Id).ToHashSet();
        if (request.ImageIds.Count != current.Count || !request.ImageIds.ToHashSet().SetEquals(current))
        {
            throw AppException.Validation("Image list doesn't match.", "Send every image id of this product exactly once.");
        }

        await unitOfWork.ExecuteInTransactionAsync(_ =>
        {
            for (var i = 0; i < request.ImageIds.Count; i++)
            {
                product.Images.Single(image => image.Id == request.ImageIds[i]).SortOrder = i;
            }

            audit.Record(staffId, AuditActions.ImageReorder, "product", productId.ToString(), new { request.ImageIds });
            return Task.CompletedTask;
        }, cancellationToken);

        return mapper.Map<AdminProductResponse>(product);
    }

    // ---------- Helpers ----------

    private async Task ApplyAsync(ProductCategory category, SaveProductCategoryRequest request, CancellationToken cancellationToken)
    {
        category.Name = request.Name.Trim();
        category.SortOrder = request.SortOrder;
        category.IsActive = request.IsActive;
        category.Slug = await ResolveSlugAsync(request.Slug, request.Name, category.Slug,
            s => categories.SlugExistsAsync(s, category.Id, cancellationToken));
    }

    private async Task ApplyAsync(Product product, SaveProductRequest request, CancellationToken cancellationToken)
    {
        var category = await categories.FindAsync(request.CategoryId, cancellationToken)
            ?? throw AppException.Validation("Category not found.", "Pick an existing category.");

        product.CategoryId = category.Id;
        product.Category = category;
        product.Name = request.Name.Trim();
        product.Description = request.Description.Trim();
        product.PriceEgp = request.PriceEgp;
        product.PricePoints = request.PricePoints;
        product.RewardPoints = request.RewardPoints;
        product.IsActive = request.IsActive;
        product.Slug = await ResolveSlugAsync(request.Slug, request.Name, product.Slug,
            s => products.SlugExistsAsync(s, product.Id, cancellationToken));
    }

    private static void Apply(ProductVariant variant, SaveProductVariantRequest request)
    {
        variant.Name = request.Name.Trim();
        variant.Sku = request.Sku.Trim();
        variant.Stock = request.Stock;
        variant.PriceOverride = request.PriceOverride;
    }

    /// <summary>
    /// An explicit slug must be free (409 otherwise). Without one, an existing slug is kept,
    /// and a new item gets one made from its name, with "-2", "-3"… added if needed.
    /// </summary>
    private static async Task<string> ResolveSlugAsync(string? requested, string name, string current, Func<string, Task<bool>> isTaken)
    {
        if (!string.IsNullOrWhiteSpace(requested))
        {
            if (requested != current && await isTaken(requested))
            {
                throw AppException.Conflict("Slug already used.", $"\"{requested}\" belongs to another item. Choose a different one.");
            }

            return requested;
        }

        return string.IsNullOrEmpty(current) ? await Slug.MakeUniqueAsync(Slug.From(name), isTaken) : current;
    }

    private async Task EnsureSkuIsFreeAsync(string sku, Guid? exceptVariantId, CancellationToken cancellationToken)
    {
        if (await products.SkuExistsAsync(sku.Trim(), exceptVariantId, cancellationToken))
        {
            throw AppException.Conflict("SKU already used.", $"\"{sku}\" belongs to another variant.");
        }
    }

    private async Task<ProductCategory> FindCategoryAsync(Guid id, CancellationToken cancellationToken) =>
        await categories.FindAsync(id, cancellationToken) ?? throw AppException.NotFound("Category not found.");

    private async Task<Product> FindProductAsync(Guid id, CancellationToken cancellationToken) =>
        await products.FindForEditAsync(id, cancellationToken) ?? throw AppException.NotFound("Product not found.");

    private static ProductVariant FindVariant(Product product, Guid variantId) =>
        product.Variants.SingleOrDefault(v => v.Id == variantId) ?? throw AppException.NotFound("Variant not found.");

    private static object Snapshot(Product p) =>
        new { p.Name, p.Slug, p.CategoryId, p.Description, p.PriceEgp, p.PricePoints, p.RewardPoints, p.IsActive };

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max].TrimEnd('-');

    private Task ValidateAsync<T>(T request, CancellationToken cancellationToken) =>
        services.GetRequiredService<IValidator<T>>().ValidateAndThrowAsync(request, cancellationToken);
}
