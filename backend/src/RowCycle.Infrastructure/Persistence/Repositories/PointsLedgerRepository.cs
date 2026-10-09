using Microsoft.EntityFrameworkCore;
using RowCycle.Application.Common;
using RowCycle.Application.Points;
using RowCycle.Domain.Entities;
using RowCycle.Domain.Enums;

namespace RowCycle.Infrastructure.Persistence.Repositories;

internal sealed class PointsLedgerRepository(AppDbContext db) : IPointsLedgerRepository
{
    public async Task<UserProfile?> LockBalanceAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("The balance can only be locked inside a transaction.");
        }

        // Drop any copy already loaded in this request so we read the balance fresh, under the lock.
        if (db.UserProfiles.Local.FirstOrDefault(p => p.UserId == userId) is { } tracked)
        {
            db.Entry(tracked).State = EntityState.Detached;
        }

        return await db.UserProfiles
            .FromSql($"SELECT * FROM user_profiles WHERE user_id = {userId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
    }

    public void Add(PointsLedgerEntry entry) => db.PointsLedger.Add(entry);

    public Task<bool> ExistsAsync(
        PointsSourceType sourceType, Guid sourceId, PointsEntryType type, CancellationToken cancellationToken = default) =>
        db.PointsLedger.AnyAsync(e => e.SourceType == sourceType && e.SourceId == sourceId && e.Type == type, cancellationToken);

    public async Task<IReadOnlyList<PointsLedgerEntry>> GetBySourceAsync(
        PointsSourceType sourceType, Guid sourceId, CancellationToken cancellationToken = default) =>
        await db.PointsLedger.AsNoTracking()
            .Where(e => e.SourceType == sourceType && e.SourceId == sourceId)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<PagedResult<PointsLedgerEntry>> GetHistoryAsync(
        Guid userId, PageRequest page, CancellationToken cancellationToken = default) =>
        db.PointsLedger.AsNoTracking()
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.Id)
            .ToPagedResultAsync(page, cancellationToken);
}
