using RowCycle.Application.Common;
using RowCycle.Domain.Entities;

namespace RowCycle.Application.Catalog;

public interface IProductCategoryRepository : IRepository<ProductCategory>
{
    /// <summary>Ordered by sort order, then name.</summary>
    Task<IReadOnlyList<ProductCategory>> ListAsync(bool activeOnly, CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(string slug, Guid? exceptId = null, CancellationToken cancellationToken = default);

    /// <summary>True if any product (active or not, but not deleted) is in the category.</summary>
    Task<bool> HasProductsAsync(Guid categoryId, CancellationToken cancellationToken = default);
}
