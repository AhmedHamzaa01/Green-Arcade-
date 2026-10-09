using RowCycle.Application.Common;

namespace RowCycle.Infrastructure.Persistence.Repositories;

internal class Repository<T>(AppDbContext db) : IRepository<T>
    where T : class
{
    protected AppDbContext Db { get; } = db;

    public async Task<T?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
        await Db.Set<T>().FindAsync([id], cancellationToken);

    public void Add(T entity) => Db.Set<T>().Add(entity);

    public void Remove(T entity) => Db.Set<T>().Remove(entity);
}
