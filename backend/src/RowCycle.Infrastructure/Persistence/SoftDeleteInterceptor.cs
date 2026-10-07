using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RowCycle.Domain.Common;

namespace RowCycle.Infrastructure.Persistence;

/// <summary>
/// Turns deletes of <see cref="ISoftDeletable"/> entities into an update of <c>deleted_at</c>,
/// and refuses to delete <see cref="INonDeletable"/> history rows.
/// </summary>
internal sealed class SoftDeleteInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void Apply(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        foreach (var entry in context.ChangeTracker.Entries().Where(e => e.State == EntityState.Deleted).ToList())
        {
            switch (entry.Entity)
            {
                case INonDeletable:
                    throw new InvalidOperationException(
                        $"{entry.Metadata.ClrType.Name} rows are history and can't be deleted.");
                case ISoftDeletable softDeletable:
                    entry.State = EntityState.Modified;
                    softDeletable.DeletedAt = now;
                    break;
            }
        }
    }
}
