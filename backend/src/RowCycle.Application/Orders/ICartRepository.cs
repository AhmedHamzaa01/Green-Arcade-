using RowCycle.Domain.Entities;

namespace RowCycle.Application.Orders;

public interface ICartRepository
{
    Task<Cart?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Cart> GetOrCreateByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ProductVariant?> FindVariantWithProductAsync(Guid variantId, CancellationToken cancellationToken = default);
    void RemoveItem(CartItem item);
    void ClearCart(Cart cart);
}

