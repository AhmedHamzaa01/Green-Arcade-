using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using RowCycle.Application.Auth;
using RowCycle.Infrastructure.Identity;

namespace RowCycle.Infrastructure.Auth;

/// <summary>ASP.NET Identity behind <see cref="IIdentityService"/>: password hashing, lockout, roles and one-time tokens.</summary>
internal sealed class IdentityService(UserManager<AppUser> userManager) : IIdentityService
{
    public async Task<UserAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        ToAccount(await userManager.FindByEmailAsync(email));

    public async Task<UserAccount?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        ToAccount(await userManager.FindByIdAsync(userId.ToString()));

    public async Task<IdentityOutcome> CreateUserAsync(
        Guid userId, string email, string password, string role, CancellationToken cancellationToken = default)
    {
        var user = new AppUser { Id = userId, UserName = email, Email = email };
        var created = ToOutcome(await userManager.CreateAsync(user, password));
        return created.Succeeded ? ToOutcome(await userManager.AddToRoleAsync(user, role)) : created;
    }

    public async Task<PasswordCheck> CheckPasswordAsync(Guid userId, string password, CancellationToken cancellationToken = default)
    {
        var user = await GetAsync(userId);
        if (await userManager.IsLockedOutAsync(user))
        {
            return PasswordCheck.LockedOut;
        }

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            await userManager.AccessFailedAsync(user);
            return PasswordCheck.Invalid;
        }

        await userManager.ResetAccessFailedCountAsync(user);
        return PasswordCheck.Valid;
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken cancellationToken = default) =>
        (await userManager.GetRolesAsync(await GetAsync(userId))).ToList();

    public async Task<string> CreateEmailVerificationTokenAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Encode(await userManager.GenerateEmailConfirmationTokenAsync(await GetAsync(userId)));

    public async Task<IdentityOutcome> ConfirmEmailAsync(Guid userId, string token, CancellationToken cancellationToken = default)
    {
        if (!TryDecode(token, out var decoded))
        {
            return new IdentityOutcome(IdentityOutcomeKind.InvalidToken);
        }

        return ToOutcome(await userManager.ConfirmEmailAsync(await GetAsync(userId), decoded));
    }

    public async Task<string> CreatePasswordResetTokenAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Encode(await userManager.GeneratePasswordResetTokenAsync(await GetAsync(userId)));

    public async Task<IdentityOutcome> ResetPasswordAsync(
        Guid userId, string token, string newPassword, CancellationToken cancellationToken = default)
    {
        if (!TryDecode(token, out var decoded))
        {
            return new IdentityOutcome(IdentityOutcomeKind.InvalidToken);
        }

        // Resetting changes the security stamp, which invalidates the token.
        var user = await GetAsync(userId);
        var outcome = ToOutcome(await userManager.ResetPasswordAsync(user, decoded, newPassword));
        if (outcome.Succeeded)
        {
            await userManager.SetLockoutEndDateAsync(user, null);
        }

        return outcome;
    }

    private async Task<AppUser> GetAsync(Guid userId) =>
        await userManager.FindByIdAsync(userId.ToString())
        ?? throw new InvalidOperationException($"User {userId} not found.");

    private static UserAccount? ToAccount(AppUser? user) =>
        user is null ? null : new UserAccount(user.Id, user.Email!, user.EmailConfirmed, user.IsActive);

    private static IdentityOutcome ToOutcome(IdentityResult result)
    {
        if (result.Succeeded)
        {
            return IdentityOutcome.Success;
        }

        var codes = result.Errors.Select(e => e.Code).ToHashSet();
        if (codes.Contains(nameof(IdentityErrorDescriber.DuplicateEmail)) || codes.Contains(nameof(IdentityErrorDescriber.DuplicateUserName)))
        {
            return new IdentityOutcome(IdentityOutcomeKind.DuplicateEmail);
        }

        if (codes.Contains(nameof(IdentityErrorDescriber.InvalidToken)))
        {
            return new IdentityOutcome(IdentityOutcomeKind.InvalidToken);
        }

        return new IdentityOutcome(IdentityOutcomeKind.Rejected, string.Join(" ", result.Errors.Select(e => e.Description)));
    }

    /// <summary>Identity tokens contain characters that break URLs, so they travel Base64Url-encoded.</summary>
    private static string Encode(string token) => WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

    private static bool TryDecode(string encoded, out string token)
    {
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encoded));
            return true;
        }
        catch (FormatException)
        {
            token = string.Empty;
            return false;
        }
    }
}
