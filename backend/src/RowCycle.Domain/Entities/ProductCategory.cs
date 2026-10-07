using RowCycle.Domain.Common;

namespace RowCycle.Domain.Entities;

public class ProductCategory : ISoftDeletable
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTimeOffset? DeletedAt { get; set; }
}
