namespace RowCycle.Application.Dtos;

/// <summary>The staff home screen (F8).</summary>
/// <param name="NewOrders">Orders still Placed, i.e. waiting to be confirmed.</param>
/// <param name="LowStockCount">All low-stock variants; <paramref name="LowStock"/> shows the lowest ones first.</param>
public sealed record DashboardResponse(
    int PendingSubmissions,
    int NewOrders,
    int LowStockThreshold,
    int LowStockCount,
    IReadOnlyList<LowStockItem> LowStock);

public sealed record LowStockItem(Guid ProductId, string ProductName, Guid VariantId, string VariantName, string Sku, int Stock);
