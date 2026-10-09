using RowCycle.Domain.Entities;

namespace RowCycle.Application.Settings;

/// <summary>Settings are keyed by name (e.g. <c>daily_submission_limit</c>), not by a Guid.</summary>
public interface ISettingsRepository
{
    Task<Setting?> FindAsync(string key, CancellationToken cancellationToken = default);

    void Add(Setting setting);
}
