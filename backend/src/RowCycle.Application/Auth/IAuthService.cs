using RowCycle.Application.Dtos;

namespace RowCycle.Application.Auth;

/// <summary>Account and session use cases (F1, FR-01–FR-04).</summary>
public interface IAuthService
{
    /// <summary>Creates the user, profile and Member role, then emails a verification link.</summary>
    Task RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default);

    Task<AuthSession> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>Swaps a refresh token for a new pair. Reusing a spent token revokes all of the user's sessions.</summary>
    Task<AuthSession> RefreshAsync(string? refreshToken, CancellationToken cancellationToken = default);

    /// <summary>Revokes the refresh token, if there is one. Always succeeds.</summary>
    Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default);

    /// <summary>Always succeeds, so the response doesn't reveal whether the email is registered.</summary>
    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);

    Task<MeResponse> GetMeAsync(Guid userId, CancellationToken cancellationToken = default);
}
