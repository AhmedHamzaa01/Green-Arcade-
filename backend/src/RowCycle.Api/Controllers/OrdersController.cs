using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RowCycle.Api.Authorization;
using RowCycle.Application.Dtos;
using RowCycle.Application.Orders;

namespace RowCycle.Api.Controllers;

/// <summary>Order placement and checkout (F6, F7).</summary>
[ApiController]
[Route("orders")]
[Authorize]
[Produces("application/json")]
public sealed class OrdersController(IOrderService orderService) : ControllerBase
{
    /// <summary>Checks out the current user's cart (Points or Cash on delivery).</summary>
    [HttpPost]
    [Authorize(Policy = Policies.VerifiedMember)]
    [ProducesResponseType<OrderDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<OrderDetailResponse> Checkout(
        [FromBody] CheckoutRequest request, CancellationToken cancellationToken) =>
        orderService.CheckoutAsync(User.GetUserId(), request, cancellationToken);
}

