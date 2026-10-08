namespace RowCycle.Application.Auth;

public sealed record RegisterRequest(string FullName, string Email, string Password);

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);

public sealed record VerifyEmailRequest(Guid UserId, string Token);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);

/// <summary>Returned by login and refresh. The access token is a JWT (FR-03).</summary>
public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);

public sealed record MeResponse(
    Guid Id,
    string Email,
    bool EmailVerified,
    string FullName,
    string? PhotoUrl,
    string? Phone,
    int PointsBalance,
    IReadOnlyList<string> Roles);
