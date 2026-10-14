using RowCycle.Application.Badges;
using RowCycle.Application.Common;
using RowCycle.Domain.Entities;
using RowCycle.Domain.Enums;
using RowCycle.Domain.Rules;

namespace RowCycle.Application.Orders;

/// <summary>
/// Moves an order to a new status with everything that goes with it. Must run inside the caller's transaction
/// with the order already locked (FR-16, FR-17, FR-18, rule 2).
/// </summary>
internal sealed class OrderWorkflow(
    IOrderRepository orders, Points.IPointsService points, BadgeAwarder awarder, TimeProvider timeProvider)
{
    public async Task MoveAsync(Order order, OrderStatus to, Guid changedBy, CancellationToken cancellationToken)
    {
        if (!OrderStatusFlow.CanMove(order.Status, to))
        {
            throw AppException.Conflict("Status change not allowed.", $"An order that is {order.Status} can't become {to}.");
        }

        switch (to)
        {
            case OrderStatus.Cancelled:
                // FR-17: stock goes back; a points order gets its points back. No rewards were paid yet.
                await RestoreStockAsync(order, cancellationToken);
                if (order.PaymentMethod == PaymentMethod.Points && order.TotalPoints > 0)
                {
                    await points.ReverseAsync(PointsSourceType.Order, order.Id, $"Order {order.OrderNumber} cancelled", changedBy, cancellationToken);
                }

                break;

            case OrderStatus.Delivered when order.TotalRewardPoints > 0:
                // FR-18: reward points for both payment methods; the ledger allows one Earn per order.
                await points.EarnAsync(order.UserId, order.TotalRewardPoints, PointsSourceType.Order, order.Id,
                    $"Order {order.OrderNumber} delivered", changedBy, cancellationToken);
                break;
        }

        orders.AddHistory(new OrderStatusHistory
        {
            Id = Guid.CreateVersion7(),
            OrderId = order.Id,
            FromStatus = order.Status,
            ToStatus = to,
            ChangedBy = changedBy,
            ChangedAt = timeProvider.GetUtcNow(),
        });
        order.Status = to;

        // FR-12: after the status is set, so the "first order" rule sees this delivery. Any order counts, with or without rewards.
        if (to == OrderStatus.Delivered)
        {
            await awarder.AwardAsync(order.UserId, BadgeTrigger.OrderDelivered, cancellationToken);
        }
    }

    private async Task RestoreStockAsync(Order order, CancellationToken cancellationToken)
    {
        var variants = await orders.LockVariantsAsync(order.Items.Select(i => i.VariantId).Distinct().ToList(), cancellationToken);
        foreach (var item in order.Items)
        {
            // Restored even if the product was deleted meanwhile; the row still exists.
            variants.Single(v => v.Id == item.VariantId).Stock += item.Quantity;
        }
    }
}
