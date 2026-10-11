using RowCycle.Domain.Entities;

namespace RowCycle.Application.Cart;

public interface ICartRepository
{
    /// <summary>
    /// The user's cart with each line's variant, product, category and images.
    /// Lines whose variant was deleted are left out. Tracked unless <paramref name="readOnly"/>.
    /// </summary>
    Task<Domain.Entities.Cart?> FindByUserAsync(Guid userId, bool readOnly, CancellationToken cancellationToken = default);

    /// <summary>A variant (not deleted) with its product, category and images; null if it doesn't exist.</summary>
    Task<ProductVariant?> FindVariantAsync(Guid variantId, CancellationToken cancellationToken = default);

    void Add(Domain.Entities.Cart cart);
    void AddItem(CartItem item);
    void RemoveItem(CartItem item);

    /// <summary>Deletes every line of the cart (used after checkout).</summary>
    Task ClearAsync(Guid cartId, CancellationToken cancellationToken = default);
}
