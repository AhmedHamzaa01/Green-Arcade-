using Microsoft.EntityFrameworkCore;
using RowCycle.Application.Settings;
using RowCycle.Domain.Entities;

namespace RowCycle.Infrastructure.Persistence.Repositories;

internal sealed class SettingsRepository(AppDbContext db) : ISettingsRepository
{
    public Task<Setting?> FindAsync(string key, CancellationToken cancellationToken = default) =>
        db.Settings.SingleOrDefaultAsync(s => s.Key == key, cancellationToken);

    public void Add(Setting setting) => db.Settings.Add(setting);
}
