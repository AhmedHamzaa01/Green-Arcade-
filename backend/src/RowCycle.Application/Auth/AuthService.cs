using System.Text.Encodings.Web;
using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Application.Mapping;
using RowCycle.Application.Users;
using RowCycle.Domain.Constants;
using RowCycle.Domain.Entities;

namespace RowCycle.Application.Auth;

/// <summary>
/// The account and session rules (F1, FR-01–FR-04). Identity, token and database details sit behind the injected interfaces.
/// </summary>
internal sealed class AuthService(
    IIdentityService identity,
    IRefreshTokenStore refreshTokens,
    IAccessTokenIssuer accessTokens,
    IUserProfileRepository profiles,
    IUnitOfWork unitOfWork,
    IEmailSender emailSender,
    IOptions<AppOptions> appOptions,
    TimeProvider timeProvider,
    IMapper mapper,
    IServiceProvider services,
    ILogger<AuthService> logger) : IAuthService
{
    private static readonly AppException InvalidCredentials =
        AppException.Unauthorized("Invalid email or password.");

    private static readonly AppException InvalidRefreshToken =
        AppException.Unauthorized("Invalid refresh token.", "Log in again.");

    private static readonly AppException InvalidLink =
        AppException.Validation("Invalid or expired link.", "Request a new link and try again.");

    private static readonly AppException EmailTaken =
        AppException.Conflict("Email already registered.", "Log in or reset your password.");

    public async Task RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);

        var email = request.Email.Trim();
        if (await identity.FindByEmailAsync(email, cancellationToken) is not null)
        {
            throw EmailTaken;
        }

        // Every new account is a Member. Other roles are given by an admin, never at sign-up.
        var userId = Guid.CreateVersion7();
        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            EnsureSucceeded(await identity.CreateUserAsync(userId, email, request.Password, Roles.Member, ct));
            profiles.Add(new UserProfile { UserId = userId, FullName = request.FullName.Trim() });
        }, cancellationToken);

        await SendVerificationEmailAsync(userId, email, cancellationToken);
    }

    public async Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);

        if (await identity.FindByIdAsync(request.UserId, cancellationToken) is null
            || !(await identity.ConfirmEmailAsync(request.UserId, request.Token, cancellationToken)).Succeeded)
        {
            throw InvalidLink;
        }
    }

    public async Task<AuthSession> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);

        var user = await identity.FindByEmailAsync(request.Email.Trim(), cancellationToken) ?? throw InvalidCredentials;

        switch (await identity.CheckPasswordAsync(user.Id, request.Password, cancellationToken))
        {
            case PasswordCheck.LockedOut:
                throw AppException.Unauthorized("Account locked.", "Too many failed attempts. Try again in 15 minutes.");
            case PasswordCheck.Invalid:
                throw InvalidCredentials;
        }

        // Unverified users may log in; the VerifiedMember policy stops them at submit and checkout (FR-02).
        if (!user.IsActive)
        {
            throw AppException.Unauthorized("Account deactivated.", "Contact Row-Cycle for help.");
        }

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthSession> RefreshAsync(string? refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw InvalidRefreshToken;
        }

        var stored = await refreshTokens.FindAsync(refreshToken, cancellationToken) ?? throw InvalidRefreshToken;

        if (stored.RevokedAt is not null)
        {
            // A spent token came back: it may have been stolen, so end every session of this user.
            logger.LogWarning("Refresh token reuse detected for user {UserId}; revoking all sessions", stored.UserId);
            await refreshTokens.RevokeAllAsync(stored.UserId, cancellationToken);
            throw InvalidRefreshToken;
        }

        if (stored.ExpiresAt <= timeProvider.GetUtcNow())
        {
            throw InvalidRefreshToken;
        }

        var user = await identity.FindByIdAsync(stored.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw InvalidRefreshToken;
        }

        // Each refresh token works once. If a parallel request already used it, treat this one as reuse.
        if (!await refreshTokens.TryRevokeAsync(stored.Id, cancellationToken))
        {
            await refreshTokens.RevokeAllAsync(stored.UserId, cancellationToken);
            throw InvalidRefreshToken;
        }

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            await refreshTokens.RevokeAsync(refreshToken, cancellationToken);
        }
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);

        // Same response whether or not the email exists, so this can't be used to find accounts.
        var user = await identity.FindByEmailAsync(request.Email.Trim(), cancellationToken);
        if (user is null || !user.IsActive)
        {
            return;
        }

        var token = await identity.CreatePasswordResetTokenAsync(user.Id, cancellationToken);
        var link = BuildLink("reset-password", ("email", user.Email), ("token", token));
        await emailSender.SendAsync(
            user.Email,
            "Reset your RowCycle password",
            $"<p>Reset your password: {Anchor(link)}</p>" +
            "<p>The link is valid for 1 hour. If you didn't ask for this, ignore this email.</p>",
            cancellationToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);

        var user = await identity.FindByEmailAsync(request.Email.Trim(), cancellationToken) ?? throw InvalidLink;

        var outcome = await identity.ResetPasswordAsync(user.Id, request.Token, request.NewPassword, cancellationToken);
        if (outcome.Kind == IdentityOutcomeKind.InvalidToken)
        {
            throw InvalidLink;
        }

        EnsureSucceeded(outcome);

        // A new password ends every existing session.
        await refreshTokens.RevokeAllAsync(user.Id, cancellationToken);
    }

    public async Task<MeResponse> GetMeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await identity.FindByIdAsync(userId, cancellationToken) ?? throw AppException.NotFound("User not found.");
        var profile = await profiles.FindAsync(userId, cancellationToken) ?? throw AppException.NotFound("Profile not found.");
        var roles = await identity.GetRolesAsync(userId, cancellationToken);

        return mapper.Map<MeResponse>(new MeSource(user, profile, roles));
    }

    private async Task<AuthSession> IssueTokensAsync(UserAccount user, CancellationToken cancellationToken)
    {
        var roles = await identity.GetRolesAsync(user.Id, cancellationToken);
        var access = accessTokens.Issue(user, roles);
        var refresh = await refreshTokens.CreateAsync(user.Id, cancellationToken);
        return new AuthSession(access.Token, access.ExpiresAt, refresh.Token, refresh.ExpiresAt);
    }

    private async Task SendVerificationEmailAsync(Guid userId, string email, CancellationToken cancellationToken)
    {
        var token = await identity.CreateEmailVerificationTokenAsync(userId, cancellationToken);
        var link = BuildLink("verify-email", ("userId", userId.ToString()), ("token", token));
        await emailSender.SendAsync(
            email,
            "Verify your RowCycle email",
            $"<p>Confirm your email: {Anchor(link)}</p><p>The link is valid for 24 hours.</p>",
            cancellationToken);
    }

    private string BuildLink(string path, params (string Key, string Value)[] query)
    {
        var parameters = string.Join("&", query.Select(q => $"{q.Key}={Uri.EscapeDataString(q.Value)}"));
        return $"{appOptions.Value.WebBaseUrl.TrimEnd('/')}/{path}?{parameters}";
    }

    private static string Anchor(string link)
    {
        var encoded = HtmlEncoder.Default.Encode(link);
        return $"<a href=\"{encoded}\">{encoded}</a>";
    }

    private Task ValidateAsync<T>(T request, CancellationToken cancellationToken) =>
        services.GetRequiredService<IValidator<T>>().ValidateAndThrowAsync(request, cancellationToken);

    private static void EnsureSucceeded(IdentityOutcome outcome)
    {
        switch (outcome.Kind)
        {
            case IdentityOutcomeKind.Succeeded:
                return;
            case IdentityOutcomeKind.DuplicateEmail:
                throw EmailTaken;
            case IdentityOutcomeKind.InvalidToken:
                throw InvalidLink;
            default:
                throw AppException.Validation("Request rejected.", outcome.Error);
        }
    }
}
