using Microsoft.AspNetCore.Identity;

namespace RowCycle.Infrastructure.Identity;

/// <summary>Identity user (asp_net_users). App data lives in <c>user_profiles</c>.</summary>
public class AppUser : IdentityUser<Guid>
{
    /// <summary>Users are deactivated, never deleted. Inactive users can't log in.</summary>
    public bool IsActive { get; set; } = true;
}
