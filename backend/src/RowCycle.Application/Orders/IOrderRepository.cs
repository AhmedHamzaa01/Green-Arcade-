using RowCycle.Application.Common;
using RowCycle.Domain.Entities;
using RowCycle.Domain.Enums;

namespace RowCycle.Application.Orders;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, bool includeDetails = true, CancellationToken cancellationToken = default);
    Task<PagedResult<Order>> ListByUserAsync(Guid userId, PageRequest page, CancellationToken cancellationToken = default);
    Task<PagedResult<Order>> ListAdminAsync(OrderStatus? status, PageRequest page, CancellationToken cancellationToken = default);
    Task<bool> OrderNumberExistsAsync(string orderNumber, CancellationToken cancellationToken = default);
    Task<ProductVariant?> FindVariantAsync(Guid variantId, CancellationToken cancellationToken = default);
    void Add(Order order);
}

