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
