namespace RowCycle.Application.Common;

/// <summary>
/// Basic operations shared by every table with a Guid key. Feature repositories extend it with their own queries.
/// Changes are written by <see cref="IUnitOfWork"/>.
/// </summary>
public interface IRepository<T>
    where T : class
{
    Task<T?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    void Add(T entity);

    /// <summary>Soft-deletes <c>ISoftDeletable</c> entities; refuses history (<c>INonDeletable</c>) entities.</summary>
    void Remove(T entity);
}
