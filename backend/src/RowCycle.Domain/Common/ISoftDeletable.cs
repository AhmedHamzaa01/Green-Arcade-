namespace RowCycle.Domain.Common;

/// <summary>
/// Deleting the entity sets <see cref="DeletedAt"/> instead of removing the row, and it is hidden from queries.
/// </summary>
public interface ISoftDeletable
{
    DateTimeOffset? DeletedAt { get; set; }
}
