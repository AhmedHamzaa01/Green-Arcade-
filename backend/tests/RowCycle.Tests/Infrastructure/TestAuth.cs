using System.Net.Http.Headers;
using System.Net.Http.Json;
using RowCycle.Application.Dtos;

namespace RowCycle.Tests.Infrastructure;

public static class TestAuth
{
    /// <summary>A client logged in as the seeded admin.</summary>
    public static async Task<HttpClient> AdminClientAsync(ApiFactory factory) =>
        await LoggedInClientAsync(factory, ApiFactory.AdminEmail, ApiFactory.AdminPassword);

    /// <summary>A client logged in as a newly registered (unverified) member.</summary>
    public static async Task<HttpClient> MemberClientAsync(ApiFactory factory)
    {
        var client = factory.CreateClient();
        var email = $"member-{Guid.NewGuid():N}@example.com";
        (await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest("Test Member", email, "Secret123")))
            .EnsureSuccessStatusCode();
        return await LoggedInClientAsync(factory, email, "Secret123", client);
    }

    /// <summary>A client logged in as a registered and email-verified member.</summary>
    public static async Task<(HttpClient Client, Guid UserId)> VerifiedMemberClientAsync(ApiFactory factory, string? email = null)
    {
        var client = factory.CreateClient();
        email ??= $"member-{Guid.NewGuid():N}@example.com";
        (await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest("Test Member", email, "Secret123")))
            .EnsureSuccessStatusCode();
        var link = factory.Emails.LastLinkQuery(email, "verify-email");
        (await client.PostAsJsonAsync("/api/v1/auth/verify-email",
            new VerifyEmailRequest(Guid.Parse(link["userId"]), link["token"])))
            .EnsureSuccessStatusCode();
        var loggedIn = await LoggedInClientAsync(factory, email, "Secret123", client);
        var me = await loggedIn.GetFromJsonAsync<MeResponse>("/api/v1/me");
        return (loggedIn, me!.Id);
    }

    private static async Task<HttpClient> LoggedInClientAsync(
        ApiFactory factory, string email, string password, HttpClient? client = null)
    {
        client ??= factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        var tokens = await response.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        return client;
    }
}
