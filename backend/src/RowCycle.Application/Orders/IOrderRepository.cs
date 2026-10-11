using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Domain.Entities;

namespace RowCycle.Application.Orders;

public interface IOrderRepository
{
    /// <summary>Next number from the database sequence: "RC-10001", "RC-10002", …</summary>
    Task<string> NextOrderNumberAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Locks the variants' rows until the transaction ends (<c>SELECT … FOR UPDATE</c>, in id order to avoid deadlocks),
    /// so two checkouts can't sell the same last item. Returned with product, category and images;
    /// soft-deleted variants/products are included so the caller can say they're gone.
    /// </summary>
    Task<IReadOnlyList<ProductVariant>> LockVariantsAsync(IReadOnlyCollection<Guid> variantIds, CancellationToken cancellationToken = default);

    void Add(Order order);

    /// <summary>Adds a status history row (always a new row; history is append-only).</summary>
    void AddHistory(OrderStatusHistory entry);

    /// <summary>Locks the order's row until the transaction ends, so two status changes can't race. Tracked, with items.</summary>
    Task<Order?> LockAsync(Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>Read-only, with items and status history.</summary>
    Task<Order?> FindAsync(Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>Newest first. <paramref name="userId"/> limits it to one member's orders.</summary>
    Task<PagedResult<Order>> SearchAsync(Guid? userId, OrderQuery query, PageRequest page, CancellationToken cancellationToken = default);
}
