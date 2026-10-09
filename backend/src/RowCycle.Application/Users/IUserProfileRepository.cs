using RowCycle.Application.Common;
using RowCycle.Domain.Entities;

namespace RowCycle.Application.Users;

/// <summary>Profiles are keyed by the user's id.</summary>
public interface IUserProfileRepository : IRepository<UserProfile>;
