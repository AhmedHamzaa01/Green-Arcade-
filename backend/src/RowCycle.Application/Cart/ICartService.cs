using RowCycle.Application.Dtos;

namespace RowCycle.Application.Cart;

/// <summary>The member's cart, kept in the database (F6). Always priced at today's prices.</summary>
public interface ICartService
{
    Task<CartResponse> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Adds to the line's quantity. Refused if the product isn't available or there isn't enough stock.</summary>
    Task<CartResponse> AddItemAsync(Guid userId, AddCartItemRequest request, CancellationToken cancellationToken = default);

    Task<CartResponse> UpdateItemAsync(Guid userId, Guid variantId, UpdateCartItemRequest request, CancellationToken cancellationToken = default);
    Task<CartResponse> RemoveItemAsync(Guid userId, Guid variantId, CancellationToken cancellationToken = default);
    Task<CartResponse> ClearAsync(Guid userId, CancellationToken cancellationToken = default);
}
