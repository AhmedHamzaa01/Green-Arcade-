using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Domain.Enums;

namespace RowCycle.Application.Points;

/// <summary>
/// The only way points change (F2, FR-05–FR-07). Each call writes one ledger row and updates the balance under a row lock,
/// in one transaction. Called inside another transaction (checkout, approval), it joins it.
/// </summary>
public interface IPointsService
{
    /// <summary>Adds points for an approved submission or a delivered order. Once per source.</summary>
    Task<PointsChange> EarnAsync(
        Guid userId, int amount, PointsSourceType sourceType, Guid sourceId,
        string? reason = null, Guid? createdBy = null, CancellationToken cancellationToken = default);

    /// <summary>Spends points on an order. Fails if the balance is too low. Once per order.</summary>
    Task<PointsChange> RedeemAsync(
        Guid userId, int amount, Guid orderId, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>Undoes everything a source did to the ledger (e.g. a cancelled points order gets its points back). Once per source.</summary>
    Task<PointsChange> ReverseAsync(
        PointsSourceType sourceType, Guid sourceId, string reason, Guid? createdBy = null, CancellationToken cancellationToken = default);

    /// <summary>Admin correction, positive or negative, with a required reason (FR-07).</summary>
    Task<PointsChange> AdjustAsync(
        Guid userId, int amount, string reason, Guid adminId, CancellationToken cancellationToken = default);

    Task<PointsHistoryResponse> GetHistoryAsync(Guid userId, PageRequest page, CancellationToken cancellationToken = default);
}
