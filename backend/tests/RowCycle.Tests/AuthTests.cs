using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using RowCycle.Api.Authorization;
using RowCycle.Application.Auth;
using RowCycle.Application.Dtos;
using RowCycle.Domain.Constants;
using RowCycle.Infrastructure.Identity;
using RowCycle.Tests.Infrastructure;

namespace RowCycle.Tests;

[Collection(ApiCollection.Name)]
public sealed class AuthTests(ApiFactory factory)
{
    private const string Password = "Secret123";

    // Cookies are handled by hand so tests can replay an old refresh token on purpose.
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

    [Fact]
    public async Task Register_verify_login_refresh_works_end_to_end()
    {
        var email = NewEmail();
        await RegisterAsync(email);

        var tokens = await LoginAsync(email, Password);
        var me = await GetMeAsync(tokens.AccessToken);
        Assert.Equal(email, me.Email);
        Assert.Equal("Test Member", me.FullName);
        Assert.False(me.EmailVerified);
        Assert.Equal(0, me.PointsBalance);
        Assert.Equal([Roles.Member], me.Roles);

        await VerifyEmailAsync(email);

        var refreshed = await RefreshAsync(tokens.RefreshToken);
        Assert.NotEqual(tokens.RefreshToken, refreshed.RefreshToken);
        Assert.True((await GetMeAsync(refreshed.AccessToken)).EmailVerified);
    }

    [Fact]
    public async Task Refresh_token_is_only_in_a_secure_httponly_cookie()
    {
        var email = NewEmail();
        await RegisterAsync(email);

        var response = await PostLoginAsync(email, Password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"), c => c.StartsWith("rc_refresh=")).ToLowerInvariant();
        Assert.Contains("httponly", cookie);
        Assert.Contains("samesite=strict", cookie);
        Assert.Contains("path=/api/v1/auth", cookie);
        Assert.Contains("expires=", cookie);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.TryGetProperty("accessToken", out _));
        Assert.False(body.RootElement.TryGetProperty("refreshToken", out _));
    }

    [Fact]
    public async Task Refresh_without_a_cookie_returns_401()
    {
        var response = await _client.PostAsync("/api/v1/auth/refresh", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Access_token_lasts_15_minutes_and_refresh_token_7_days()
    {
        var email = NewEmail();
        await RegisterAsync(email);

        var before = DateTimeOffset.UtcNow;
        var tokens = await LoginAsync(email, Password);

        Assert.InRange(tokens.AccessTokenExpiresAt, before.AddMinutes(14), before.AddMinutes(16));
        Assert.InRange(tokens.RefreshTokenExpiresAt, before.AddDays(7).AddMinutes(-1), before.AddDays(7).AddMinutes(1));
    }

    [Fact]
    public async Task Unverified_member_is_blocked_by_the_verified_member_policy()
    {
        var email = NewEmail();
        await RegisterAsync(email);
        var tokens = await LoginAsync(email, Password);

        Assert.False(await IsAllowedAsync(tokens.AccessToken, Policies.VerifiedMember));

        await VerifyEmailAsync(email);
        var refreshed = await RefreshAsync(tokens.RefreshToken);

        Assert.True(await IsAllowedAsync(refreshed.AccessToken, Policies.VerifiedMember));
        Assert.False(await IsAllowedAsync(refreshed.AccessToken, Policies.Admin));
    }

    [Fact]
    public async Task Reusing_a_refresh_token_ends_every_session()
    {
        var email = NewEmail();
        await RegisterAsync(email);
        var first = await LoginAsync(email, Password);
        var second = await RefreshAsync(first.RefreshToken);

        Assert.Equal(HttpStatusCode.Unauthorized, (await PostRefreshAsync(first.RefreshToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await PostRefreshAsync(second.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task Logout_revokes_the_refresh_token()
    {
        var email = NewEmail();
        await RegisterAsync(email);
        var tokens = await LoginAsync(email, Password);

        using var logout = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        logout.Headers.Add("Cookie", $"rc_refresh={tokens.RefreshToken}");
        var response = await _client.SendAsync(logout);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var deleted = Assert.Single(response.Headers.GetValues("Set-Cookie"), c => c.StartsWith("rc_refresh="));
        Assert.Contains("expires=Thu, 01 Jan 1970", deleted, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.Unauthorized, (await PostRefreshAsync(tokens.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task Password_reset_changes_the_password_once_and_ends_sessions()
    {
        var email = NewEmail();
        await RegisterAsync(email);
        var session = await LoginAsync(email, Password);

        var forgot = await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new ForgotPasswordRequest(email));
        Assert.Equal(HttpStatusCode.NoContent, forgot.StatusCode);
        var link = factory.Emails.LastLinkQuery(email, "reset-password");

        var reset = await _client.PostAsJsonAsync("/api/v1/auth/reset-password",
            new ResetPasswordRequest(email, link["token"], "NewSecret456"));
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await PostLoginAsync(email, Password)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await PostLoginAsync(email, "NewSecret456")).StatusCode);

        var reuse = await _client.PostAsJsonAsync("/api/v1/auth/reset-password",
            new ResetPasswordRequest(email, link["token"], "Another789"));
        Assert.Equal(HttpStatusCode.BadRequest, reuse.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await PostRefreshAsync(session.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task Forgot_password_for_unknown_email_returns_204_and_sends_nothing()
    {
        var email = NewEmail();

        var response = await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new ForgotPasswordRequest(email));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(factory.Emails.SentTo(email));
    }

    [Theory]
    [InlineData("short1")]
    [InlineData("onlyletters")]
    [InlineData("12345678")]
    public async Task Register_rejects_weak_passwords(string password)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest("Test Member", NewEmail(), password));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty("password", out _));
    }

    [Fact]
    public async Task Register_rejects_a_duplicate_email()
    {
        var email = NewEmail();
        await RegisterAsync(email);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest("Someone Else", email.ToUpperInvariant(), Password));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Login_with_a_wrong_password_returns_401_problem_details()
    {
        var email = NewEmail();
        await RegisterAsync(email);

        var response = await PostLoginAsync(email, "WrongPass1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Invalid email or password.", body.RootElement.GetProperty("title").GetString());
        Assert.True(body.RootElement.TryGetProperty("correlationId", out _));
    }

    [Fact]
    public async Task Deactivated_user_cannot_log_in()
    {
        var email = NewEmail();
        await RegisterAsync(email);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = await users.FindByEmailAsync(email);
            user!.IsActive = false;
            await users.UpdateAsync(user);
        }

        var response = await PostLoginAsync(email, Password);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_requires_a_token()
    {
        var response = await _client.GetAsync("/api/v1/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Configured_admin_is_seeded_with_the_admin_role()
    {
        var tokens = await LoginAsync(ApiFactory.AdminEmail, ApiFactory.AdminPassword);

        var me = await GetMeAsync(tokens.AccessToken);

        Assert.True(me.EmailVerified);
        Assert.Contains(Roles.Admin, me.Roles);
        Assert.True(await IsAllowedAsync(tokens.AccessToken, Policies.Admin));
        Assert.True(await IsAllowedAsync(tokens.AccessToken, Policies.Moderator));
    }

    private static string NewEmail() => $"member-{Guid.NewGuid():N}@example.com";

    private async Task RegisterAsync(string email)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest("Test Member", email, Password));
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private async Task VerifyEmailAsync(string email)
    {
        var link = factory.Emails.LastLinkQuery(email, "verify-email");
        var response = await _client.PostAsJsonAsync("/api/v1/auth/verify-email",
            new VerifyEmailRequest(Guid.Parse(link["userId"]), link["token"]));
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private Task<HttpResponseMessage> PostLoginAsync(string email, string password) =>
        _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));

    private async Task<Session> LoginAsync(string email, string password)
    {
        var response = await PostLoginAsync(email, password);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadSessionAsync(response);
    }

    private async Task<Session> RefreshAsync(string refreshToken)
    {
        var response = await PostRefreshAsync(refreshToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadSessionAsync(response);
    }

    /// <summary>POST /auth/refresh sending <paramref name="refreshToken"/> as the cookie, like a browser would.</summary>
    private Task<HttpResponseMessage> PostRefreshAsync(string refreshToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        request.Headers.Add("Cookie", $"rc_refresh={refreshToken}");
        return _client.SendAsync(request);
    }

    /// <summary>The access token from the body and the refresh token from the Set-Cookie header.</summary>
    private static async Task<Session> ReadSessionAsync(HttpResponseMessage response)
    {
        var body = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        var cookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("rc_refresh="));
        var parts = cookie.Split(';', StringSplitOptions.TrimEntries);
        var token = Uri.UnescapeDataString(parts[0]["rc_refresh=".Length..]);
        var expires = DateTimeOffset.Parse(parts.Single(p => p.StartsWith("expires=", StringComparison.OrdinalIgnoreCase))["expires=".Length..]);
        return new Session(body.AccessToken, body.AccessTokenExpiresAt, token, expires);
    }

    private sealed record Session(string AccessToken, DateTimeOffset AccessTokenExpiresAt, string RefreshToken, DateTimeOffset RefreshTokenExpiresAt);

    private async Task<MeResponse> GetMeAsync(string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<MeResponse>())!;
    }

    /// <summary>Validates the token exactly like the API does, then evaluates the policy against its claims.</summary>
    private async Task<bool> IsAllowedAsync(string accessToken, string policy)
    {
        var bearer = factory.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(accessToken, bearer.TokenValidationParameters);
        Assert.True(result.IsValid, result.Exception?.Message);

        var authorization = factory.Services.GetRequiredService<IAuthorizationService>();
        return (await authorization.AuthorizeAsync(new ClaimsPrincipal(result.ClaimsIdentity), policy)).Succeeded;
    }
}
