using RowCycle.Domain.Enums;

namespace RowCycle.Domain.Entities;

public class Order
{
    public Guid Id { get; set; }

    /// <summary>Human-readable number; format decided in Step 7.</summary>
    public string OrderNumber { get; set; } = string.Empty;

    public Guid UserId { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Placed;
    public decimal TotalEgp { get; set; }
    public int TotalPoints { get; set; }

    /// <summary>Σ(unit_reward_points × quantity), written to the ledger when the order is delivered (FR-18).</summary>
    public int TotalRewardPoints { get; set; }

    public ShippingAddressSnapshot ShippingAddress { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; }

    public List<OrderItem> Items { get; set; } = [];
    public List<OrderStatusHistory> StatusHistory { get; set; } = [];
}
