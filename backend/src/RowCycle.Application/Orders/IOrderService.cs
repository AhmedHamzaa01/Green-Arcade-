using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Domain.Enums;

namespace RowCycle.Application.Orders;

public interface IOrderService
{
    Task<OrderDetailResponse> CheckoutAsync(Guid userId, CheckoutRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<OrderSummaryResponse>> GetMyOrdersAsync(Guid userId, PageRequest page, CancellationToken cancellationToken = default);
    Task<OrderDetailResponse> GetMyOrderByIdAsync(Guid userId, Guid orderId, CancellationToken cancellationToken = default);
    Task<OrderDetailResponse> CancelMyOrderAsync(Guid userId, Guid orderId, CancellationToken cancellationToken = default);
    Task<PagedResult<OrderSummaryResponse>> GetAdminOrdersAsync(OrderStatus? status, PageRequest page, CancellationToken cancellationToken = default);
    Task<OrderDetailResponse> GetAdminOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<OrderDetailResponse> UpdateOrderStatusAsync(Guid orderId, Guid staffId, UpdateOrderStatusRequest request, CancellationToken cancellationToken = default);
}

