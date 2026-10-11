using Microsoft.EntityFrameworkCore;
using RowCycle.Application.Cart;
using RowCycle.Domain.Entities;

namespace RowCycle.Infrastructure.Persistence.Repositories;

internal sealed class CartRepository(AppDbContext db) : ICartRepository
{
    public Task<Cart?> FindByUserAsync(Guid userId, bool readOnly, CancellationToken cancellationToken = default)
    {
        IQueryable<Cart> carts = db.Carts
            .Include(c => c.Items).ThenInclude(i => i.Variant).ThenInclude(v => v!.Product).ThenInclude(p => p!.Category)
            .Include(c => c.Items).ThenInclude(i => i.Variant).ThenInclude(v => v!.Product).ThenInclude(p => p!.Images)
            .AsSplitQuery();
        if (readOnly)
        {
            carts = carts.AsNoTracking();
        }

        return carts.SingleOrDefaultAsync(c => c.UserId == userId, cancellationToken);
    }

    public Task<ProductVariant?> FindVariantAsync(Guid variantId, CancellationToken cancellationToken = default) =>
        db.ProductVariants.AsNoTracking()
            .Include(v => v.Product).ThenInclude(p => p!.Category)
            .Include(v => v.Product).ThenInclude(p => p!.Images)
            .SingleOrDefaultAsync(v => v.Id == variantId, cancellationToken);

    public void Add(Cart cart) => db.Carts.Add(cart);

    public void AddItem(CartItem item) => db.CartItems.Add(item);

    public void RemoveItem(CartItem item) => db.CartItems.Remove(item);

    public Task ClearAsync(Guid cartId, CancellationToken cancellationToken = default) =>
        db.CartItems.Where(i => i.CartId == cartId).ExecuteDeleteAsync(cancellationToken);
}
