using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RowCycle.Api.Authorization;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Application.Orders;
using RowCycle.Domain.Enums;

namespace RowCycle.Api.Controllers.Admin;

/// <summary>Process orders (StoreManager, Admin). Status changes are audit-logged.</summary>
[ApiController]
[Route("admin/orders")]
[Authorize(Policy = Policies.StoreManager)]
[Produces("application/json")]
public sealed class AdminOrdersController(IOrderAdminService orders) : ControllerBase
{
    /// <summary>Newest first; filter by status, search by order number or recipient name.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<OrderSummaryResponse>>(StatusCodes.Status200OK)]
    public Task<PagedResult<OrderSummaryResponse>> Search(
        CancellationToken cancellationToken,
        [FromQuery] OrderStatus? status = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize) =>
        orders.SearchAsync(new OrderQuery(status, search), new PageRequest(page, pageSize), cancellationToken);

    [HttpGet("{id:guid}")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<OrderResponse> Get(Guid id, CancellationToken cancellationToken) => orders.GetAsync(id, cancellationToken);

    /// <summary>Move to the next status (see <c>nextStatuses</c>). Delivered pays reward points; Cancelled returns stock and points.</summary>
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<OrderResponse> ChangeStatus(Guid id, ChangeOrderStatusRequest request, CancellationToken cancellationToken) =>
        orders.ChangeStatusAsync(id, request, User.GetUserId(), cancellationToken);
}
