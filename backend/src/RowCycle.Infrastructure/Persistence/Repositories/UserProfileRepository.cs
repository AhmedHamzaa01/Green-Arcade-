using RowCycle.Application.Users;
using RowCycle.Domain.Entities;

namespace RowCycle.Infrastructure.Persistence.Repositories;

internal sealed class UserProfileRepository(AppDbContext db) : Repository<UserProfile>(db), IUserProfileRepository;
