using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RowCycle.Application.Cart;
using RowCycle.Application.Dtos;

namespace RowCycle.Api.Controllers;

/// <summary>The signed-in member's cart (F6). Every call returns the whole cart at today's prices.</summary>
[ApiController]
[Route("cart")]
[Authorize]
[Produces("application/json")]
public sealed class CartController(ICartService cart) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<CartResponse>(StatusCodes.Status200OK)]
    public Task<CartResponse> Get(CancellationToken cancellationToken) => cart.GetAsync(User.GetUserId(), cancellationToken);

    /// <summary>Adds to the quantity already in the cart. 409 if not available or not enough stock.</summary>
    [HttpPost("items")]
    [ProducesResponseType<CartResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<CartResponse> AddItem(AddCartItemRequest request, CancellationToken cancellationToken) =>
        cart.AddItemAsync(User.GetUserId(), request, cancellationToken);

    [HttpPut("items/{variantId:guid}")]
    [ProducesResponseType<CartResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<CartResponse> UpdateItem(Guid variantId, UpdateCartItemRequest request, CancellationToken cancellationToken) =>
        cart.UpdateItemAsync(User.GetUserId(), variantId, request, cancellationToken);

    [HttpDelete("items/{variantId:guid}")]
    [ProducesResponseType<CartResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<CartResponse> RemoveItem(Guid variantId, CancellationToken cancellationToken) =>
        cart.RemoveItemAsync(User.GetUserId(), variantId, cancellationToken);

    [HttpDelete]
    [ProducesResponseType<CartResponse>(StatusCodes.Status200OK)]
    public Task<CartResponse> Clear(CancellationToken cancellationToken) => cart.ClearAsync(User.GetUserId(), cancellationToken);
}
