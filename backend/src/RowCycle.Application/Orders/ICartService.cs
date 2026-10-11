using RowCycle.Application.Dtos;

namespace RowCycle.Application.Orders;

public interface ICartService
{
    Task<CartResponse> GetCartAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<CartResponse> AddItemAsync(Guid userId, AddToCartRequest request, CancellationToken cancellationToken = default);
    Task<CartResponse> UpdateItemAsync(Guid userId, Guid variantId, UpdateCartItemRequest request, CancellationToken cancellationToken = default);
    Task<CartResponse> RemoveItemAsync(Guid userId, Guid variantId, CancellationToken cancellationToken = default);
    Task ClearCartAsync(Guid userId, CancellationToken cancellationToken = default);
}

