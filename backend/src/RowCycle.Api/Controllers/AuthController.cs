using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RowCycle.Application.Auth;
using RowCycle.Application.Dtos;

namespace RowCycle.Api.Controllers;

/// <summary>Account and session endpoints (F1, FR-01–FR-04). Rate-limited per client IP (NFR-03).</summary>
[ApiController]
[Route("auth")]
[EnableRateLimiting(RateLimitPolicies.Auth)]
[Produces("application/json")]
public sealed class AuthController(IAuthService auth, IWebHostEnvironment environment) : ControllerBase
{
    /// <summary>Cookies are HTTPS-only except in local Development (plain http://localhost).</summary>
    private bool SecureCookies => !environment.IsDevelopment();

    /// <summary>Create an account. A verification link is emailed.</summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        await auth.RegisterAsync(request, cancellationToken);
        return NoContent();
    }

    /// <summary>Confirm the email with the userId and token from the verification link.</summary>
    [HttpPost("verify-email")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmail(VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        await auth.VerifyEmailAsync(request, cancellationToken);
        return NoContent();
    }

    /// <summary>Returns an access token (15 min) and sets the refresh token cookie (7 days).</summary>
    [HttpPost("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<AuthResponse> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var session = await auth.LoginAsync(request, cancellationToken);
        RefreshTokenCookie.Write(Response, session, SecureCookies);
        return session.ToResponse();
    }

    /// <summary>Uses the refresh token cookie to get a new access token and a new cookie. Each refresh token works once.</summary>
    [HttpPost("refresh")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<AuthResponse> Refresh(CancellationToken cancellationToken)
    {
        var session = await auth.RefreshAsync(RefreshTokenCookie.Read(Request), cancellationToken);
        RefreshTokenCookie.Write(Response, session, SecureCookies);
        return session.ToResponse();
    }

    /// <summary>Ends this session: revokes the refresh token and deletes the cookie.</summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await auth.LogoutAsync(RefreshTokenCookie.Read(Request), cancellationToken);
        RefreshTokenCookie.Delete(Response, SecureCookies);
        return NoContent();
    }

    /// <summary>Email a password reset link. Always returns 204, whether or not the email is registered.</summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await auth.ForgotPasswordAsync(request, cancellationToken);
        return NoContent();
    }

    /// <summary>Set a new password with the token from the reset link. Ends all sessions.</summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await auth.ResetPasswordAsync(request, cancellationToken);
        return NoContent();
    }
}
