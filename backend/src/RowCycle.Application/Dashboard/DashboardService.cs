using RowCycle.Application.Dtos;
using RowCycle.Application.Settings;

namespace RowCycle.Application.Dashboard;

/// <summary>Counts for the staff home screen (F8).</summary>
public interface IDashboardService
{
    Task<DashboardResponse> GetAsync(CancellationToken cancellationToken = default);
}

/// <summary>The dashboard's database reads, implemented in Infrastructure.</summary>
public interface IDashboardQueries
{
    Task<int> CountPendingSubmissionsAsync(CancellationToken cancellationToken = default);

    Task<int> CountPlacedOrdersAsync(CancellationToken cancellationToken = default);

    /// <summary>Variants of active, non-deleted products with stock at or below <paramref name="threshold"/>, lowest first.</summary>
    Task<(int Total, IReadOnlyList<LowStockItem> Items)> LowStockAsync(int threshold, int take, CancellationToken cancellationToken = default);
}

internal sealed class DashboardService(IDashboardQueries queries, ISettingsService settings) : IDashboardService
{
    /// <summary>How many low-stock lines the dashboard shows; the count covers all of them.</summary>
    public const int LowStockShown = 20;

    public async Task<DashboardResponse> GetAsync(CancellationToken cancellationToken = default)
    {
        var threshold = await settings.GetLowStockThresholdAsync(cancellationToken);
        var (lowStockCount, lowStock) = await queries.LowStockAsync(threshold, LowStockShown, cancellationToken);
        return new DashboardResponse(
            await queries.CountPendingSubmissionsAsync(cancellationToken),
            await queries.CountPlacedOrdersAsync(cancellationToken),
            threshold,
            lowStockCount,
            lowStock);
    }
}
