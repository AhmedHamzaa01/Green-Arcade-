using Microsoft.EntityFrameworkCore;
using RowCycle.Application.Users;
using RowCycle.Domain.Entities;

namespace RowCycle.Infrastructure.Persistence.Repositories;

internal sealed class UserProfileRepository(AppDbContext db) : IUserProfileRepository
{
    public void Add(UserProfile profile) => db.UserProfiles.Add(profile);

    public Task<UserProfile?> FindAsync(Guid userId, CancellationToken cancellationToken = default) =>
        db.UserProfiles.AsNoTracking().SingleOrDefaultAsync(p => p.UserId == userId, cancellationToken);
}
