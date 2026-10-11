using Microsoft.EntityFrameworkCore;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Application.Orders;
using RowCycle.Domain.Entities;

namespace RowCycle.Infrastructure.Persistence.Repositories;

internal sealed class OrderRepository(AppDbContext db) : IOrderRepository
{
    public async Task<string> NextOrderNumberAsync(CancellationToken cancellationToken = default)
    {
        // The sequence name is a constant from the code, not user input, so it's safe in the SQL text.
        var next = await db.Database
            .SqlQueryRaw<long>($"SELECT nextval('{AppDbContext.OrderNumberSequence}') AS \"Value\"")
            .SingleAsync(cancellationToken);
        return $"RC-{next}";
    }

    public async Task<IReadOnlyList<ProductVariant>> LockVariantsAsync(
        IReadOnlyCollection<Guid> variantIds, CancellationToken cancellationToken = default)
    {
        EnsureTransaction();
        var ids = variantIds.Distinct().OrderBy(id => id).ToArray();

        // Forget any copies loaded earlier in this request, so the stock is read fresh under the lock.
        foreach (var tracked in db.ProductVariants.Local.Where(v => ids.Contains(v.Id)).ToList())
        {
            db.Entry(tracked).State = EntityState.Detached;
        }

        // Plain SQL because LINQ can't say FOR UPDATE; ORDER BY id so every caller locks in the same order (no deadlocks).
        var variants = await db.ProductVariants
            .FromSql($"SELECT * FROM product_variants WHERE id = ANY({ids}) ORDER BY id FOR UPDATE")
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken);

        // Product details for the checks; deleted products are loaded too so the caller can tell they're gone.
        var productIds = variants.Select(v => v.ProductId).Distinct().ToArray();
        await db.Products.IgnoreQueryFilters()
            .Where(p => productIds.Contains(p.Id))
            .Include(p => p.Category)
            .Include(p => p.Images.Where(i => i.DeletedAt == null))
            .AsSplitQuery()
            .LoadAsync(cancellationToken);

        return variants;
    }

    public void Add(Order order) => db.Orders.Add(order);

    public void AddHistory(OrderStatusHistory entry) => db.OrderStatusHistory.Add(entry);

    public async Task<Order?> LockAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        EnsureTransaction();
        var order = await db.Orders
            .FromSql($"SELECT * FROM orders WHERE id = {orderId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (order is not null)
        {
            await db.Entry(order).Collection(o => o.Items).LoadAsync(cancellationToken);
        }

        return order;
    }

    public Task<Order?> FindAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            .AsSplitQuery()
            .SingleOrDefaultAsync(o => o.Id == orderId, cancellationToken);

    public Task<PagedResult<Order>> SearchAsync(Guid? userId, OrderQuery query, PageRequest page, CancellationToken cancellationToken = default)
    {
        var orders = db.Orders.AsNoTracking().Include(o => o.Items).AsQueryable();

        if (userId is { } id)
        {
            orders = orders.Where(o => o.UserId == id);
        }

        if (query.Status is { } status)
        {
            orders = orders.Where(o => o.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = "%" + query.Search.Trim().Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_") + "%";
            orders = orders.Where(o => EF.Functions.ILike(o.OrderNumber, pattern, @"\")
                || EF.Functions.ILike(o.ShippingAddress.FullName, pattern, @"\"));
        }

        return orders
            .OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.Id)
            .AsSplitQuery()
            .ToPagedResultAsync(page, cancellationToken);
    }

    private void EnsureTransaction()
    {
        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("Rows can only be locked inside a transaction.");
        }
    }
}
