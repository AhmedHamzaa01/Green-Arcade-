namespace RowCycle.Application.Auth;

/// <summary>
/// Accounts and passwords, implemented with ASP.NET Identity in Infrastructure.
/// Tokens passed in and out are already URL-safe.
/// </summary>
public interface IIdentityService
{
    Task<UserAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<UserAccount?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Creates the login with a hashed password and gives it <paramref name="role"/>.</summary>
    Task<IdentityOutcome> CreateUserAsync(Guid userId, string email, string password, string role, CancellationToken cancellationToken = default);

    /// <summary>Checks the password and updates the failed-attempt counter (5 failures lock the account for 15 minutes).</summary>
    Task<PasswordCheck> CheckPasswordAsync(Guid userId, string password, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<string> CreateEmailVerificationTokenAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IdentityOutcome> ConfirmEmailAsync(Guid userId, string token, CancellationToken cancellationToken = default);

    Task<string> CreatePasswordResetTokenAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Sets the new password and clears any lockout. A token works only once.</summary>
    Task<IdentityOutcome> ResetPasswordAsync(Guid userId, string token, string newPassword, CancellationToken cancellationToken = default);
}

public sealed record UserAccount(Guid Id, string Email, bool EmailConfirmed, bool IsActive);

public enum PasswordCheck
{
    Valid,
    Invalid,
    LockedOut,
}

public enum IdentityOutcomeKind
{
    Succeeded,
    DuplicateEmail,
    InvalidToken,
    Rejected,
}

/// <summary>Result of an Identity operation. <see cref="Error"/> explains a <see cref="IdentityOutcomeKind.Rejected"/> result.</summary>
public sealed record IdentityOutcome(IdentityOutcomeKind Kind, string? Error = null)
{
    public static readonly IdentityOutcome Success = new(IdentityOutcomeKind.Succeeded);

    public bool Succeeded => Kind == IdentityOutcomeKind.Succeeded;
}
