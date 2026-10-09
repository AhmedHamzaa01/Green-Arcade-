namespace RowCycle.Application.Dtos;

public sealed record RegisterRequest(string FullName, string Email, string Password);

public sealed record LoginRequest(string Email, string Password);

public sealed record VerifyEmailRequest(Guid UserId, string Token);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);

/// <summary>
/// Body of login and refresh: the access token (JWT, 15 min, FR-03). The refresh token is not in the body;
/// it travels only in an httpOnly cookie that page scripts can't read.
/// </summary>
public sealed record AuthResponse(string AccessToken, DateTimeOffset AccessTokenExpiresAt);

/// <summary>What login and refresh produce. The API puts the refresh token in a cookie and returns <see cref="AuthResponse"/>.</summary>
public sealed record AuthSession(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt)
{
    public AuthResponse ToResponse() => new(AccessToken, AccessTokenExpiresAt);
}

public sealed record MeResponse(
    Guid Id,
    string Email,
    bool EmailVerified,
    string FullName,
    string? PhotoUrl,
    string? Phone,
    int PointsBalance,
    IReadOnlyList<string> Roles);
