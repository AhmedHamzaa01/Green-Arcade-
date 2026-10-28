using Microsoft.EntityFrameworkCore;
using RowCycle.Application.Dashboard;
using RowCycle.Application.Dtos;
using RowCycle.Domain.Enums;

namespace RowCycle.Infrastructure.Persistence.Repositories;

internal sealed class DashboardQueries(AppDbContext db) : IDashboardQueries
{
    public Task<int> CountPendingSubmissionsAsync(CancellationToken cancellationToken = default) =>
        db.Submissions.CountAsync(s => s.Status == SubmissionStatus.Pending, cancellationToken);

    public Task<int> CountPlacedOrdersAsync(CancellationToken cancellationToken = default) =>
        db.Orders.CountAsync(o => o.Status == OrderStatus.Placed, cancellationToken);

    public async Task<(int Total, IReadOnlyList<LowStockItem> Items)> LowStockAsync(
        int threshold, int take, CancellationToken cancellationToken = default)
    {
        // The soft-delete filters already hide deleted variants and products; inactive products aren't sold, so skip them.
        var lowStock = db.ProductVariants.AsNoTracking()
            .Where(v => v.Stock <= threshold && v.Product!.IsActive);

        var total = await lowStock.CountAsync(cancellationToken);
        var items = await lowStock
            .OrderBy(v => v.Stock).ThenBy(v => v.Product!.Name).ThenBy(v => v.Name)
            .Take(take)
            .Select(v => new LowStockItem(v.ProductId, v.Product!.Name, v.Id, v.Name, v.Sku, v.Stock))
            .ToListAsync(cancellationToken);
        return (total, items);
    }
}
