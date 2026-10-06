using System.Net;
using System.Text.Json;
using RowCycle.Tests.Infrastructure;

namespace RowCycle.Tests;

[Collection(ApiCollection.Name)]
public sealed class HealthTests(ApiFactory factory)
{
    [Fact]
    public async Task Health_returns_healthy_when_database_is_reachable()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Healthy", body.RootElement.GetProperty("status").GetString());
        Assert.Equal("Healthy", body.RootElement.GetProperty("checks").GetProperty("postgres").GetString());
    }

    [Fact]
    public async Task Response_echoes_incoming_correlation_id()
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("X-Correlation-Id", "test-123");

        var response = await client.SendAsync(request);

        Assert.Equal("test-123", Assert.Single(response.Headers.GetValues("X-Correlation-Id")));
    }

    [Fact]
    public async Task Response_creates_correlation_id_when_missing()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.False(string.IsNullOrWhiteSpace(Assert.Single(response.Headers.GetValues("X-Correlation-Id"))));
    }
}
