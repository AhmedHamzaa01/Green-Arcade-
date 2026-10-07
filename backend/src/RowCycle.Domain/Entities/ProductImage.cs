using RowCycle.Domain.Common;

namespace RowCycle.Domain.Entities;

public class ProductImage : ISoftDeletable
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string Url { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}
