using Microsoft.EntityFrameworkCore;
using RowCycle.Application.Common;
using RowCycle.Application.Orders;
using RowCycle.Domain.Entities;
using RowCycle.Domain.Enums;

namespace RowCycle.Infrastructure.Persistence.Repositories;

internal sealed class OrderRepository(AppDbContext db) : IOrderRepository
{
    public async Task<Order?> GetByIdAsync(Guid id, bool includeDetails = true, CancellationToken cancellationToken = default)
    {
        IQueryable<Order> query = db.Orders;
        if (includeDetails)
        {
            query = query
                .Include(o => o.Items)
                .Include(o => o.StatusHistory.OrderByDescending(h => h.ChangedAt));
        }

        return await query.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<PagedResult<Order>> ListByUserAsync(Guid userId, PageRequest page, CancellationToken cancellationToken = default)
    {
        var query = db.Orders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt);

        return await query
            .Include(o => o.Items)
            .ToPagedResultAsync(page, cancellationToken);
    }

    public async Task<PagedResult<Order>> ListAdminAsync(OrderStatus? status, PageRequest page, CancellationToken cancellationToken = default)
    {
        var query = db.Orders.AsQueryable();
        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        query = query.OrderByDescending(o => o.CreatedAt);

        return await query
            .Include(o => o.Items)
            .ToPagedResultAsync(page, cancellationToken);
    }

    public async Task<bool> OrderNumberExistsAsync(string orderNumber, CancellationToken cancellationToken = default) =>
        await db.Orders.AnyAsync(o => o.OrderNumber == orderNumber, cancellationToken);

    public async Task<ProductVariant?> FindVariantAsync(Guid variantId, CancellationToken cancellationToken = default) =>
        await db.ProductVariants.FirstOrDefaultAsync(v => v.Id == variantId, cancellationToken);

    public void Add(Order order) => db.Orders.Add(order);
}

