using RowCycle.Application.Common;
using RowCycle.Application.Dtos;

namespace RowCycle.Application.Orders;

/// <summary>Checkout and a member's own orders (F6, F7).</summary>
public interface IOrderService
{
    /// <summary>
    /// Turns the cart into an order in ONE transaction (FR-14): checks availability and stock, copies prices (FR-15),
    /// reduces stock, takes points for a points order, logs "Placed", and empties the cart. All or nothing.
    /// </summary>
    Task<OrderResponse> CheckoutAsync(Guid userId, CheckoutRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<OrderSummaryResponse>> ListMineAsync(Guid userId, PageRequest page, CancellationToken cancellationToken = default);

    /// <summary>404 for orders that belong to someone else, the same as for missing ones.</summary>
    Task<OrderResponse> GetMineAsync(Guid userId, Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>Members can cancel only while the order is still Placed.</summary>
    Task<OrderResponse> CancelMineAsync(Guid userId, Guid orderId, CancellationToken cancellationToken = default);
}

/// <summary>Order handling for staff (PRD F8). Every status change is audit-logged.</summary>
public interface IOrderAdminService
{
    Task<PagedResult<OrderSummaryResponse>> SearchAsync(OrderQuery query, PageRequest page, CancellationToken cancellationToken = default);
    Task<OrderResponse> GetAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<OrderResponse> ChangeStatusAsync(Guid orderId, ChangeOrderStatusRequest request, Guid staffId, CancellationToken cancellationToken = default);
}
