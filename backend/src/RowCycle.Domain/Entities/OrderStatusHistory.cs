using RowCycle.Domain.Common;
using RowCycle.Domain.Enums;

namespace RowCycle.Domain.Entities;

public class OrderStatusHistory : IAppendOnly
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }

    /// <summary>Null for the first row (order placed).</summary>
    public OrderStatus? FromStatus { get; set; }

    public OrderStatus ToStatus { get; set; }
    public Guid? ChangedBy { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
}
