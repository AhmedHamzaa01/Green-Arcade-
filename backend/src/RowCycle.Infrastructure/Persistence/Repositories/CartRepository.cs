using Microsoft.EntityFrameworkCore;
using RowCycle.Application.Orders;
using RowCycle.Domain.Entities;

namespace RowCycle.Infrastructure.Persistence.Repositories;

internal sealed class CartRepository(AppDbContext db) : ICartRepository
{
    public async Task<Cart?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await db.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.Variant)
                    .ThenInclude(v => v!.Product)
                        .ThenInclude(p => p!.Images)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

    public async Task<Cart> GetOrCreateByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cart = await GetByUserIdAsync(userId, cancellationToken);
        if (cart is null)
        {
            cart = new Cart
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                Items = [],
            };
            db.Carts.Add(cart);
        }

        return cart;
    }

    public async Task<ProductVariant?> FindVariantWithProductAsync(Guid variantId, CancellationToken cancellationToken = default) =>
        await db.ProductVariants
            .Include(v => v.Product)
                .ThenInclude(p => p!.Images)
            .FirstOrDefaultAsync(v => v.Id == variantId, cancellationToken);

    public void RemoveItem(CartItem item) => db.CartItems.Remove(item);

    public void ClearCart(Cart cart)
    {
        db.CartItems.RemoveRange(cart.Items);
        cart.Items.Clear();
    }
}

