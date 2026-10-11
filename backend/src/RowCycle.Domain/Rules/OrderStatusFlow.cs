using RowCycle.Domain.Enums;

namespace RowCycle.Domain.Rules;

/// <summary>
/// The only allowed order status moves (FR-16):
/// Placed → Confirmed → Shipped → Delivered, and Placed/Confirmed → Cancelled. Delivered and Cancelled are final.
/// </summary>
public static class OrderStatusFlow
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> Next = new()
    {
        [OrderStatus.Placed] = [OrderStatus.Confirmed, OrderStatus.Cancelled],
        [OrderStatus.Confirmed] = [OrderStatus.Shipped, OrderStatus.Cancelled],
        [OrderStatus.Shipped] = [OrderStatus.Delivered],
        [OrderStatus.Delivered] = [],
        [OrderStatus.Cancelled] = [],
    };

    public static IReadOnlyList<OrderStatus> NextStatuses(OrderStatus from) => Next[from];

    public static bool CanMove(OrderStatus from, OrderStatus to) => Next[from].Contains(to);

    /// <summary>Members may cancel their own order only before staff confirm it.</summary>
    public static bool MemberCanCancel(OrderStatus status) => status == OrderStatus.Placed;
}
