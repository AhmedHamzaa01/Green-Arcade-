using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RowCycle.Application.Dtos;
using RowCycle.Application.Orders;

namespace RowCycle.Api.Controllers;

/// <summary>Cart management for signed-in members (F6).</summary>
[ApiController]
[Route("cart")]
[Authorize]
[Produces("application/json")]
public sealed class CartController(ICartService cartService) : ControllerBase
{
    /// <summary>Current user's cart.</summary>
    [HttpGet]
    [ProducesResponseType<CartResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<CartResponse> GetCart(CancellationToken cancellationToken) =>
        cartService.GetCartAsync(User.GetUserId(), cancellationToken);

    /// <summary>Adds a variant to the cart or increments its quantity.</summary>
    [HttpPost("items")]
    [ProducesResponseType<CartResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<CartResponse> AddItem(
        [FromBody] AddToCartRequest request, CancellationToken cancellationToken) =>
        cartService.AddItemAsync(User.GetUserId(), request, cancellationToken);

    /// <summary>Sets quantity for a variant in the cart. If quantity is 0, the item is removed.</summary>
    [HttpPut("items/{variantId:guid}")]
    [ProducesResponseType<CartResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<CartResponse> UpdateItem(
        [FromRoute] Guid variantId,
        [FromBody] UpdateCartItemRequest request,
        CancellationToken cancellationToken) =>
        cartService.UpdateItemAsync(User.GetUserId(), variantId, request, cancellationToken);

    /// <summary>Removes a variant from the cart.</summary>
    [HttpDelete("items/{variantId:guid}")]
    [ProducesResponseType<CartResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<CartResponse> RemoveItem(
        [FromRoute] Guid variantId, CancellationToken cancellationToken) =>
        cartService.RemoveItemAsync(User.GetUserId(), variantId, cancellationToken);

    /// <summary>Clears all items from the cart.</summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ClearCart(CancellationToken cancellationToken)
    {
        await cartService.ClearCartAsync(User.GetUserId(), cancellationToken);
        return NoContent();
    }
}

