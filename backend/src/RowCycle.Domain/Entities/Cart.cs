namespace RowCycle.Domain.Entities;

/// <summary>One cart per user.</summary>
public class Cart
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public List<CartItem> Items { get; set; } = [];
}
