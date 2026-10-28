using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Domain.Enums;
using RowCycle.Infrastructure.Persistence;
using RowCycle.Tests.Infrastructure;

namespace RowCycle.Tests;

/// <summary>Step 10: admin users — roles, blocking (immediate), manual points (FR-07, FR-19).</summary>
[Collection(ApiCollection.Name)]
public sealed class UserAdminTests(ApiFactory factory)
{
    private const string Password = "Secret123";   // TestAuth's password
    private static readonly JsonSerializerOptions Json = TestJson.Options;

    // Cookies by hand, so a test can hold a member's refresh token like their browser would.
    private readonly HttpClient _anonymous = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

    [Fact]
    public async Task Blocking_stops_the_current_token_refresh_and_login_and_unblocking_allows_login_again()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var (member, userId) = await TestAuth.VerifiedMemberAsync(factory);
        var email = (await TestShop.MeAsync(member)).Email;
        var session = await LoginAsync(email);

        var blocked = await admin.PutAsJsonAsync($"/api/v1/admin/users/{userId}/block", new SetBlockedRequest(true));
        Assert.Equal(HttpStatusCode.OK, blocked.StatusCode);
        Assert.False((await blocked.Content.ReadFromJsonAsync<AdminUserDetail>(Json))!.IsActive);

        // Immediately: the access token issued before the block no longer works…
        Assert.Equal(HttpStatusCode.Unauthorized, (await member.GetAsync("/api/v1/me")).StatusCode);
        // …no new one can be had with the refresh token…
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(session.RefreshToken)).StatusCode);
        // …and logging in is refused.
        var login = await _anonymous.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, Password));
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.Equal("Account deactivated.", await TitleAsync(login));

        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/v1/admin/users/{userId}/block", new SetBlockedRequest(false))).StatusCode);
        var again = await LoginAsync(email);
        Assert.Equal(HttpStatusCode.OK, (await GetWithTokenAsync("/api/v1/me", again.AccessToken)).StatusCode);

        var log = await AuditActionsAsync(admin, userId);
        Assert.Contains("user.block", log);
        Assert.Contains("user.unblock", log);
    }

    [Fact]
    public async Task Role_changes_apply_at_once_and_a_refresh_brings_the_new_roles()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var (member, userId) = await TestAuth.VerifiedMemberAsync(factory);
        var email = (await TestShop.MeAsync(member)).Email;
        var session = await LoginAsync(email);
        Assert.Equal(HttpStatusCode.Forbidden, (await GetWithTokenAsync("/api/v1/admin/submissions", session.AccessToken)).StatusCode);

        var changed = await admin.PutAsJsonAsync($"/api/v1/admin/users/{userId}/roles", new SetRolesRequest(["Member", "Moderator"]));
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        Assert.Equal(["Member", "Moderator"], (await changed.Content.ReadFromJsonAsync<AdminUserDetail>(Json))!.Roles);

        // The old token (no Moderator role in it) is refused; the web app then refreshes and retries.
        Assert.Equal(HttpStatusCode.Unauthorized, (await GetWithTokenAsync("/api/v1/me", session.AccessToken)).StatusCode);
        var refreshed = await ReadSessionAsync(await RefreshAsync(session.RefreshToken));
        Assert.Equal(HttpStatusCode.OK, (await GetWithTokenAsync("/api/v1/admin/submissions", refreshed.AccessToken)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await GetWithTokenAsync("/api/v1/admin/dashboard", refreshed.AccessToken)).StatusCode);

        // Back to Member only: moderation is closed again after the next refresh.
        await admin.PutAsJsonAsync($"/api/v1/admin/users/{userId}/roles", new SetRolesRequest(["Member"]));
        var demoted = await ReadSessionAsync(await RefreshAsync(refreshed.RefreshToken));
        Assert.Equal(HttpStatusCode.Forbidden, (await GetWithTokenAsync("/api/v1/admin/submissions", demoted.AccessToken)).StatusCode);

        Assert.Equal(2, (await AuditActionsAsync(admin, userId)).Count(a => a == "user.roles"));
    }

    [Fact]
    public async Task Roles_are_validated_and_saving_the_same_roles_changes_nothing()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var (member, userId) = await TestAuth.VerifiedMemberAsync(factory);

        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync($"/api/v1/admin/users/{userId}/roles", new SetRolesRequest([]))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.PutAsJsonAsync($"/api/v1/admin/users/{userId}/roles", new SetRolesRequest(["Member", "Superuser"]))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await admin.PutAsJsonAsync($"/api/v1/admin/users/{Guid.NewGuid()}/roles", new SetRolesRequest(["Member"]))).StatusCode);

        // Same roles: no audit row, and the member's session isn't interrupted.
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/v1/admin/users/{userId}/roles", new SetRolesRequest(["Member"]))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await member.GetAsync("/api/v1/me")).StatusCode);
        Assert.DoesNotContain("user.roles", await AuditActionsAsync(admin, userId));
    }

    [Fact]
    public async Task Points_adjustment_needs_a_reason_shows_in_the_ledger_with_the_admin_and_cannot_go_below_zero()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var (member, userId) = await TestAuth.VerifiedMemberAsync(factory);

        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.PostAsJsonAsync($"/api/v1/admin/users/{userId}/points-adjustments", new AdjustPointsRequest(50, " "))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.PostAsJsonAsync($"/api/v1/admin/users/{userId}/points-adjustments", new AdjustPointsRequest(0, "Nothing"))).StatusCode);

        var added = await admin.PostAsJsonAsync($"/api/v1/admin/users/{userId}/points-adjustments", new AdjustPointsRequest(50, "Event bonus"));
        Assert.Equal(HttpStatusCode.Created, added.StatusCode);
        Assert.Equal(50, (await added.Content.ReadFromJsonAsync<PointsChange>())!.Balance);

        var tooMuch = await admin.PostAsJsonAsync($"/api/v1/admin/users/{userId}/points-adjustments", new AdjustPointsRequest(-60, "Mistake"));
        Assert.Equal(HttpStatusCode.Conflict, tooMuch.StatusCode);

        // The member sees it in their history, with the reason.
        var history = (await member.GetFromJsonAsync<PointsHistoryResponse>("/api/v1/me/points", Json))!;
        Assert.Equal(50, history.Balance);
        var entry = Assert.Single(history.Items);
        Assert.Equal((50, PointsEntryType.Adjust, PointsSourceType.Manual, "Event bonus"), (entry.Amount, entry.Type, entry.SourceType, entry.Reason));

        // FR-07: the ledger row stores which admin made it.
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var createdBy = await db.PointsLedger.Where(e => e.Id == entry.Id).Select(e => e.CreatedBy).SingleAsync();
            Assert.Equal(ApiFactory.AdminUserId(factory), createdBy);
        }

        // The admin sees the same history on the user's page.
        var adminView = (await admin.GetFromJsonAsync<PointsHistoryResponse>($"/api/v1/admin/users/{userId}/points", Json))!;
        Assert.Equal(entry.Id, Assert.Single(adminView.Items).Id);
        Assert.Contains("points.adjust", await AuditActionsAsync(admin, userId));
    }

    [Fact]
    public async Task Users_can_be_searched_by_email_and_filtered_by_role_and_status()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var (member, userId) = await TestAuth.VerifiedMemberAsync(factory);
        var email = (await TestShop.MeAsync(member)).Email;

        var found = await SearchAsync(admin, $"search={Uri.EscapeDataString(email.ToUpperInvariant())}");
        var row = Assert.Single(found.Items);
        Assert.Equal((userId, "Test Buyer", true, true), (row.Id, row.FullName, row.IsActive, row.EmailVerified));
        Assert.Equal(["Member"], row.Roles);

        await admin.PutAsJsonAsync($"/api/v1/admin/users/{userId}/roles", new SetRolesRequest(["Member", "StoreManager"]));
        Assert.Contains((await SearchAsync(admin, "role=StoreManager&pageSize=100")).Items, u => u.Id == userId);
        Assert.DoesNotContain((await SearchAsync(admin, "blocked=true&pageSize=100")).Items, u => u.Id == userId);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync("/api/v1/admin/users?role=Superuser")).StatusCode);

        var detail = (await admin.GetFromJsonAsync<AdminUserDetail>($"/api/v1/admin/users/{userId}", Json))!;
        Assert.Equal((0, 0, 0, 0), (detail.ApprovedSubmissions, detail.PendingSubmissions, detail.Orders, detail.Badges));
    }

    [Fact]
    public async Task Only_admins_can_manage_users()
    {
        var (member, userId) = await TestAuth.VerifiedMemberAsync(factory);

        Assert.Equal(HttpStatusCode.Forbidden, (await member.GetAsync("/api/v1/admin/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await member.PutAsJsonAsync($"/api/v1/admin/users/{userId}/roles", new SetRolesRequest(["Admin"]))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await member.PostAsJsonAsync($"/api/v1/admin/users/{userId}/points-adjustments", new AdjustPointsRequest(1000, "Me"))).StatusCode);
    }

    // ---------- Helpers ----------

    private sealed record Session(string AccessToken, string RefreshToken);

    private async Task<Session> LoginAsync(string email)
    {
        var response = await _anonymous.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, Password));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadSessionAsync(response);
    }

    private Task<HttpResponseMessage> RefreshAsync(string refreshToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        request.Headers.Add("Cookie", $"rc_refresh={refreshToken}");
        return _anonymous.SendAsync(request);
    }

    private static async Task<Session> ReadSessionAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        var cookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("rc_refresh="));
        return new Session(body.AccessToken, cookie.Split(';')[0]["rc_refresh=".Length..]);
    }

    private Task<HttpResponseMessage> GetWithTokenAsync(string url, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return _anonymous.SendAsync(request);
    }

    private static async Task<PagedResult<AdminUserListItem>> SearchAsync(HttpClient admin, string query) =>
        (await admin.GetFromJsonAsync<PagedResult<AdminUserListItem>>($"/api/v1/admin/users?{query}", Json))!;

    private static async Task<List<string>> AuditActionsAsync(HttpClient admin, Guid userId) =>
        (await admin.GetFromJsonAsync<PagedResult<AuditLogResponse>>($"/api/v1/admin/audit-logs?entity=user&entityId={userId}&pageSize=100"))!
            .Items.Select(e => e.Action).ToList();

    private static async Task<string?> TitleAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("title").GetString();
}
