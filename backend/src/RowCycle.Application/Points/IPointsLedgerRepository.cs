using RowCycle.Application.Common;
using RowCycle.Domain.Entities;
using RowCycle.Domain.Enums;

namespace RowCycle.Application.Points;

/// <summary>Ledger storage. Rows are only ever added (FR-05).</summary>
public interface IPointsLedgerRepository
{
    /// <summary>
    /// Loads the user's profile and locks its row until the transaction ends (<c>SELECT ... FOR UPDATE</c>, FR-06).
    /// Other points changes for the same user wait; other users are not affected.
    /// </summary>
    Task<UserProfile?> LockBalanceAsync(Guid userId, CancellationToken cancellationToken = default);

    void Add(PointsLedgerEntry entry);

    Task<bool> ExistsAsync(PointsSourceType sourceType, Guid sourceId, PointsEntryType type, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PointsLedgerEntry>> GetBySourceAsync(PointsSourceType sourceType, Guid sourceId, CancellationToken cancellationToken = default);

    /// <summary>The user's entries, newest first.</summary>
    Task<PagedResult<PointsLedgerEntry>> GetHistoryAsync(Guid userId, PageRequest page, CancellationToken cancellationToken = default);
}
