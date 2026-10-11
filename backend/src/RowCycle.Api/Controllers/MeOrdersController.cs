using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Application.Orders;

namespace RowCycle.Api.Controllers;

/// <summary>The signed-in member's orders (F7).</summary>
[ApiController]
[Route("me/orders")]
[Authorize]
[Produces("application/json")]
public sealed class MeOrdersController(IOrderService orders) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<OrderSummaryResponse>>(StatusCodes.Status200OK)]
    public Task<PagedResult<OrderSummaryResponse>> List(
        CancellationToken cancellationToken, [FromQuery] int page = 1, [FromQuery] int pageSize = PageRequest.DefaultPageSize) =>
        orders.ListMineAsync(User.GetUserId(), new PageRequest(page, pageSize), cancellationToken);

    [HttpGet("{id:guid}")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<OrderResponse> Get(Guid id, CancellationToken cancellationToken) =>
        orders.GetMineAsync(User.GetUserId(), id, cancellationToken);

    /// <summary>Only while the order is still Placed. Stock goes back; a points order gets its points back.</summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<OrderResponse> Cancel(Guid id, CancellationToken cancellationToken) =>
        orders.CancelMineAsync(User.GetUserId(), id, cancellationToken);
}
