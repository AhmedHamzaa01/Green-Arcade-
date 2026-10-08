using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using RowCycle.Application.Auth;
using RowCycle.Tests.Infrastructure;

namespace RowCycle.Tests;

[Collection(ApiCollection.Name)]
public sealed class RateLimitTests(ApiFactory factory)
{
    [Fact]
    public async Task Auth_endpoints_return_429_after_the_limit()
    {
        using var limited = factory.WithWebHostBuilder(b => b.UseSetting("RateLimiting:Auth:PermitLimit", "3"));
        using var client = limited.CreateClient();
        var request = new LoginRequest("nobody@example.com", "Wrong1234");

        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/login", request)).StatusCode);
        }

        var blocked = await client.PostAsJsonAsync("/api/v1/auth/login", request);
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.Equal("application/problem+json", blocked.Content.Headers.ContentType?.MediaType);
    }
}
