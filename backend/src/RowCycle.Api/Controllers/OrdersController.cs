using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RowCycle.Api.Authorization;
using RowCycle.Application.Dtos;
using RowCycle.Application.Orders;

namespace RowCycle.Api.Controllers;

/// <summary>Checkout (F6, FR-14). Only members with a verified email can order (FR-02).</summary>
[ApiController]
[Route("orders")]
[Authorize(Policy = Policies.VerifiedMember)]
[Produces("application/json")]
public sealed class OrdersController(IOrderService orders) : ControllerBase
{
    /// <summary>Places an order from the cart, paid with points or cash on delivery. All or nothing.</summary>
    [HttpPost]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Checkout(CheckoutRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await orders.CheckoutAsync(User.GetUserId(), request, cancellationToken));
}
