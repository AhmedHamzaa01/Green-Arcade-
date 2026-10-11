using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RowCycle.Application.Auth;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Application.Orders;
using RowCycle.Application.Points;

namespace RowCycle.Api.Controllers;

/// <summary>The signed-in user's own data.</summary>
[ApiController]
[Route("me")]
[Authorize]
[Produces("application/json")]
public sealed class MeController(IAuthService auth, IPointsService points, IOrderService orders) : ControllerBase
{
    /// <summary>Profile, points balance and roles of the signed-in user.</summary>
    [HttpGet]
    [ProducesResponseType<MeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<MeResponse> Get(CancellationToken cancellationToken) =>
        auth.GetMeAsync(User.GetUserId(), cancellationToken);

    /// <summary>Points balance and history, newest first (F2).</summary>
    [HttpGet("points")]
    [ProducesResponseType<PointsHistoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<PointsHistoryResponse> GetPoints(
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize) =>
        points.GetHistoryAsync(User.GetUserId(), new PageRequest(page, pageSize), cancellationToken);

    /// <summary>Own order history, newest first (F7).</summary>
    [HttpGet("orders")]
    [ProducesResponseType<PagedResult<OrderSummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<PagedResult<OrderSummaryResponse>> GetMyOrders(
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize) =>
        orders.GetMyOrdersAsync(User.GetUserId(), new PageRequest(page, pageSize), cancellationToken);

    /// <summary>Own order detail with status history and snapshot items (F7).</summary>
    [HttpGet("orders/{id:guid}")]
    [ProducesResponseType<OrderDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<OrderDetailResponse> GetMyOrderById(
        [FromRoute] Guid id, CancellationToken cancellationToken) =>
        orders.GetMyOrderByIdAsync(User.GetUserId(), id, cancellationToken);

    /// <summary>Cancels an order if it is still in Placed status (F7, FR-17).</summary>
    [HttpPost("orders/{id:guid}/cancel")]
    [ProducesResponseType<OrderDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<OrderDetailResponse> CancelMyOrder(
        [FromRoute] Guid id, CancellationToken cancellationToken) =>
        orders.CancelMyOrderAsync(User.GetUserId(), id, cancellationToken);
}
