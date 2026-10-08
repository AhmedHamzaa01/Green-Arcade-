namespace RowCycle.Application.Common;

/// <summary>Saves changes, optionally as one database transaction (NFR-05).</summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Runs <paramref name="work"/>, saves, and commits; rolls everything back if anything throws.</summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken = default);
}
