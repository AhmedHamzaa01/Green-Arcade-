using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RowCycle.Api.Authorization;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Application.Orders;
using RowCycle.Domain.Enums;

namespace RowCycle.Api.Controllers.Admin;

/// <summary>Order processing for store managers and admins (F7, F8).</summary>
[ApiController]
[Route("admin/orders")]
[Authorize(Policy = Policies.StoreManager)]
[Produces("application/json")]
public sealed class AdminOrdersController(IOrderService orders) : ControllerBase
{
    /// <summary>Lists orders, newest first, filterable by status (F7, F8).</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<OrderSummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<PagedResult<OrderSummaryResponse>> ListOrders(
        CancellationToken cancellationToken,
        [FromQuery] OrderStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize) =>
        orders.GetAdminOrdersAsync(status, new PageRequest(page, pageSize), cancellationToken);

    /// <summary>Full order details including items, address, and status history.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<OrderDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<OrderDetailResponse> GetOrder(
        [FromRoute] Guid id, CancellationToken cancellationToken) =>
        orders.GetAdminOrderByIdAsync(id, cancellationToken);

    /// <summary>Updates order status along Placed → Confirmed → Shipped → Delivered or Cancelled (FR-16–FR-19).</summary>
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType<OrderDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<OrderDetailResponse> UpdateStatus(
        [FromRoute] Guid id,
        [FromBody] UpdateOrderStatusRequest request,
        CancellationToken cancellationToken) =>
        orders.UpdateOrderStatusAsync(id, User.GetUserId(), request, cancellationToken);
}

