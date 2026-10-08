using RowCycle.Domain.Entities;

namespace RowCycle.Application.Users;

public interface IUserProfileRepository
{
    /// <summary>Adds the profile; it's written on the next save.</summary>
    void Add(UserProfile profile);

    Task<UserProfile?> FindAsync(Guid userId, CancellationToken cancellationToken = default);
}
