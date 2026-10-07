using Microsoft.AspNetCore.Identity;

namespace RowCycle.Infrastructure.Identity;

/// <summary>Identity user (asp_net_users). App data lives in <c>user_profiles</c>; login features come in Step 3.</summary>
public class AppUser : IdentityUser<Guid>;
