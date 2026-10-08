namespace RowCycle.Application.Auth;

/// <summary>Account and session use cases (F1, FR-01–FR-04).</summary>
public interface IAuthService
{
    /// <summary>Creates the user, profile and Member role, then emails a verification link.</summary>
    Task RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default);

    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>Swaps a refresh token for a new pair. Reusing a spent token revokes all of the user's sessions.</summary>
    Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default);

    Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default);

    /// <summary>Always succeeds, so the response doesn't reveal whether the email is registered.</summary>
    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);

    Task<MeResponse> GetMeAsync(Guid userId, CancellationToken cancellationToken = default);
}
