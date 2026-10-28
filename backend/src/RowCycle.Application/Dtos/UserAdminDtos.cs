namespace RowCycle.Application.Dtos;

/// <summary>A row in the admin users list.</summary>
public sealed record AdminUserListItem(
    Guid Id,
    string FullName,
    string Email,
    IReadOnlyList<string> Roles,
    bool IsActive,
    bool EmailVerified,
    int PointsBalance);

/// <summary>One user with what they did, for the admin's user page.</summary>
public sealed record AdminUserDetail(
    Guid Id,
    string FullName,
    string Email,
    string? Phone,
    IReadOnlyList<string> Roles,
    bool IsActive,
    bool EmailVerified,
    int PointsBalance,
    int ApprovedSubmissions,
    int PendingSubmissions,
    int Orders,
    int Badges);

/// <summary>Filters for <c>GET /admin/users</c>. Search matches name or email.</summary>
public sealed record UserQuery(string? Search = null, string? Role = null, bool? Blocked = null);

/// <summary>The user's complete set of roles (any combination, at least one).</summary>
public sealed record SetRolesRequest(IReadOnlyList<string> Roles);

public sealed record SetBlockedRequest(bool Blocked);

/// <summary>Manual points change (FR-07): positive or negative, with a reason the member sees.</summary>
public sealed record AdjustPointsRequest(int Amount, string Reason);
