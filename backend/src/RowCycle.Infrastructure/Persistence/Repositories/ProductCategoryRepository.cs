using Microsoft.EntityFrameworkCore;
using RowCycle.Application.Catalog;
using RowCycle.Domain.Entities;

namespace RowCycle.Infrastructure.Persistence.Repositories;

internal sealed class ProductCategoryRepository(AppDbContext db) : Repository<ProductCategory>(db), IProductCategoryRepository
{
    public async Task<IReadOnlyList<ProductCategory>> ListAsync(bool activeOnly, CancellationToken cancellationToken = default) =>
        await Db.ProductCategories.AsNoTracking()
            .Where(c => !activeOnly || c.IsActive)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);

    public Task<bool> SlugExistsAsync(string slug, Guid? exceptId = null, CancellationToken cancellationToken = default) =>
        Db.ProductCategories.AnyAsync(c => c.Slug == slug && c.Id != exceptId, cancellationToken);

    public Task<bool> HasProductsAsync(Guid categoryId, CancellationToken cancellationToken = default) =>
        Db.Products.AnyAsync(p => p.CategoryId == categoryId, cancellationToken);
}
