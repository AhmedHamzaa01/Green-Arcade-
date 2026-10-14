using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using RowCycle.Application.Dtos;

namespace RowCycle.Tests.Infrastructure;

/// <summary>Submits and approves actions for tests that need approved submissions (badges, points).</summary>
public static class TestActions
{
    /// <summary>Seeded categories (SeedData).</summary>
    public static readonly Guid CanCollection = Guid.Parse("17ce849a-9c7c-46d9-b5e4-aad889192218");
    public static readonly Guid Recycling = Guid.Parse("865079da-4bd6-4715-950b-dca027c9f711");

    /// <summary>A PNG signature plus some bytes: enough for the content check.</summary>
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13, 1, 2, 3, 4];

    public static async Task<Guid> SubmitAsync(HttpClient member, Guid categoryId)
    {
        var file = new ByteArrayContent(Png);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        var form = new MultipartFormDataContent
        {
            { file, "photo", "action.png" },
            { new StringContent(categoryId.ToString()), "categoryId" },
        };
        var response = await member.PostAsync("/api/v1/submissions", form);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<SubmissionResponse>(TestJson.Options))!.Id;
    }

    public static async Task SubmitAndApproveAsync(HttpClient member, HttpClient admin, Guid categoryId)
    {
        var id = await SubmitAsync(member, categoryId);
        var approved = await admin.PostAsync($"/api/v1/admin/submissions/{id}/approve", null);
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
    }
}
