namespace RowCycle.Infrastructure.Auth;

/// <summary>Claim names in the access token (JWT).</summary>
public static class AppClaimTypes
{
    public const string UserId = "sub";
    public const string Email = "email";
    public const string Role = "role";

    /// <summary>"true" once the user confirmed their email (FR-02).</summary>
    public const string EmailVerified = "email_verified";

    /// <summary>
    /// A copy of the user's security stamp when the token was issued. Checked on every request, so blocking a user
    /// or changing their roles takes effect immediately instead of when the 15-minute token expires.
    /// </summary>
    public const string Stamp = "stamp";
}
