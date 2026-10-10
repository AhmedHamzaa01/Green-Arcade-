using Microsoft.EntityFrameworkCore;
using RowCycle.Application.Catalog;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Domain.Entities;

namespace RowCycle.Infrastructure.Persistence.Repositories;

internal sealed class ProductRepository(AppDbContext db) : Repository<Product>(db), IProductRepository
{
    public Task<PagedResult<Product>> SearchPublicAsync(ProductQuery query, PageRequest page, CancellationToken cancellationToken = default)
    {
        var products = WithDetails(Db.Products.AsNoTracking())
            .Where(p => p.IsActive && p.Category!.IsActive);

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            products = products.Where(p => p.Category!.Slug == query.Category);
        }

        products = Search(products, query.Search);

        var ordered = query.Sort switch
        {
            ProductSort.PriceAsc => products.OrderBy(p => p.PriceEgp).ThenBy(p => p.Name),
            ProductSort.PriceDesc => products.OrderByDescending(p => p.PriceEgp).ThenBy(p => p.Name),
            _ => products.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id),
        };

        return ordered.AsSplitQuery().ToPagedResultAsync(page, cancellationToken);
    }

    public Task<Product?> FindPublicBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        WithDetails(Db.Products.AsNoTracking())
            .AsSplitQuery()
            .SingleOrDefaultAsync(p => p.Slug == slug && p.IsActive && p.Category!.IsActive, cancellationToken);

    public Task<PagedResult<Product>> SearchAdminAsync(AdminProductQuery query, PageRequest page, CancellationToken cancellationToken = default)
    {
        var products = Search(WithDetails(Db.Products.AsNoTracking()), query.Search);

        if (query.CategoryId is { } categoryId)
        {
            products = products.Where(p => p.CategoryId == categoryId);
        }

        if (query.IsActive is { } isActive)
        {
            products = products.Where(p => p.IsActive == isActive);
        }

        return products
            .OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
            .AsSplitQuery()
            .ToPagedResultAsync(page, cancellationToken);
    }

    public Task<Product?> FindForEditAsync(Guid id, CancellationToken cancellationToken = default) =>
        WithDetails(Db.Products).AsSplitQuery().SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> SlugExistsAsync(string slug, Guid? exceptProductId = null, CancellationToken cancellationToken = default) =>
        Db.Products.AnyAsync(p => p.Slug == slug && p.Id != exceptProductId, cancellationToken);

    public Task<bool> SkuExistsAsync(string sku, Guid? exceptVariantId = null, CancellationToken cancellationToken = default) =>
        Db.ProductVariants.AnyAsync(v => v.Sku == sku && v.Id != exceptVariantId, cancellationToken);

    // New children are added through their DbSet: a child that already has its Guid id and is only added to
    // the parent's collection would be taken for an existing row and updated instead of inserted.
    public void AddVariant(Product product, ProductVariant variant)
    {
        variant.ProductId = product.Id;
        Db.ProductVariants.Add(variant);
    }

    public void AddImage(Product product, ProductImage image)
    {
        image.ProductId = product.Id;
        Db.ProductImages.Add(image);
    }

    public void RemoveVariant(ProductVariant variant) => Db.ProductVariants.Remove(variant);

    public void RemoveImage(ProductImage image) => Db.ProductImages.Remove(image);

    /// <summary>Soft-deleted variants and images are left out automatically by the query filters.</summary>
    private static IQueryable<Product> WithDetails(IQueryable<Product> products) =>
        products.Include(p => p.Category).Include(p => p.Variants).Include(p => p.Images);

    /// <summary>Case-insensitive "contains" on the name. % and _ typed by the user are matched literally.</summary>
    private static IQueryable<Product> Search(IQueryable<Product> products, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return products;
        }

        var pattern = "%" + search.Trim().Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_") + "%";
        return products.Where(p => EF.Functions.ILike(p.Name, pattern, @"\"));
    }
}
