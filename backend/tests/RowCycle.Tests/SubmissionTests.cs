using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Domain.Enums;
using RowCycle.Tests.Infrastructure;
using static RowCycle.Tests.Infrastructure.TestShop;

namespace RowCycle.Tests;

[Collection(ApiCollection.Name)]
public sealed class SubmissionTests(ApiFactory factory)
{
    /// <summary>The API sends enums as text ("Pending").</summary>
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    /// <summary>Seeded categories (SeedData): one that tracks weight, one that doesn't. Both give 10 points.</summary>
    private static readonly Guid CanCollection = Guid.Parse("17ce849a-9c7c-46d9-b5e4-aad889192218");
    private static readonly Guid Recycling = Guid.Parse("865079da-4bd6-4715-950b-dca027c9f711");

    /// <summary>A PNG signature plus some bytes: enough for the content check.</summary>
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13, 1, 2, 3, 4];

    [Fact]
    public async Task Categories_are_listed_with_their_points()
    {
        var (member, _) = await TestAuth.VerifiedMemberAsync(factory);

        var categories = await member.GetFromJsonAsync<List<SubmissionCategoryResponse>>("/api/v1/submission-categories", Json);

        var cans = Assert.Single(categories!, c => c.Id == CanCollection);
        Assert.Equal((10, true), (cans.PointsReward, cans.TracksWeight));
    }

    [Fact]
    public async Task Upload_creates_a_pending_submission_with_a_stored_photo()
    {
        var (member, userId) = await TestAuth.VerifiedMemberAsync(factory);

        var response = await SubmitAsync(member, CanCollection, note: "  Beach bags  ", weightKg: 3.5m);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var submission = (await response.Content.ReadFromJsonAsync<SubmissionResponse>(Json))!;
        Assert.Equal(SubmissionStatus.Pending, submission.Status);
        Assert.Equal((userId, "Can collection", "Beach bags", 3.5m), (submission.UserId, submission.CategoryName, submission.Note, submission.WeightKg));
        Assert.StartsWith("/media/submissions/", submission.PhotoUrl);
        Assert.EndsWith(".png", submission.PhotoUrl);
        Assert.Equal(HttpStatusCode.OK, (await factory.CreateClient().GetAsync(submission.PhotoUrl)).StatusCode);

        var mine = await member.GetFromJsonAsync<PagedResult<SubmissionResponse>>("/api/v1/me/submissions", Json);
        Assert.Equal(submission.Id, Assert.Single(mine!.Items).Id);
    }

    [Fact]
    public async Task A_file_that_is_not_an_image_is_refused()
    {
        var (member, _) = await TestAuth.VerifiedMemberAsync(factory);

        var response = await SubmitAsync(member, Recycling, photo: "not really a png"u8.ToArray());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Unsupported photo.", await TitleAsync(response));
    }

    [Fact]
    public async Task Weight_is_refused_for_a_category_that_does_not_track_it()
    {
        var (member, _) = await TestAuth.VerifiedMemberAsync(factory);

        var response = await SubmitAsync(member, Recycling, weightKg: 2m);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, (await member.GetFromJsonAsync<PagedResult<SubmissionResponse>>("/api/v1/me/submissions", Json))!.Total);
    }

    [Fact]
    public async Task An_unverified_member_cannot_submit()
    {
        var member = await TestAuth.MemberClientAsync(factory);

        var response = await SubmitAsync(member, Recycling);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task The_daily_limit_refuses_one_more_submission_and_rejected_ones_still_count()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var (member, _) = await TestAuth.VerifiedMemberAsync(factory);
        await SetDailyLimitAsync(admin, 2);
        try
        {
            var first = await ReadAsync(await SubmitAsync(member, Recycling));
            Assert.Equal(HttpStatusCode.OK, (await RejectAsync(admin, first.Id, "Blurry photo")).StatusCode);
            Assert.Equal(HttpStatusCode.Created, (await SubmitAsync(member, Recycling)).StatusCode);

            var third = await SubmitAsync(member, Recycling);

            Assert.Equal(HttpStatusCode.Conflict, third.StatusCode);
            Assert.Equal("Daily limit reached.", await TitleAsync(third));
            Assert.Equal(2, (await member.GetFromJsonAsync<PagedResult<SubmissionResponse>>("/api/v1/me/submissions", Json))!.Total);
        }
        finally
        {
            await SetDailyLimitAsync(admin, 5);
        }
    }

    [Fact]
    public async Task Approving_pays_the_points_once_and_a_second_approve_is_refused()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var (member, _) = await TestAuth.VerifiedMemberAsync(factory);
        var submission = await ReadAsync(await SubmitAsync(member, CanCollection));

        var approved = await admin.PostAsync($"/api/v1/admin/submissions/{submission.Id}/approve", null);

        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
        var reviewed = (await approved.Content.ReadFromJsonAsync<SubmissionResponse>(Json))!;
        Assert.Equal(SubmissionStatus.Approved, reviewed.Status);
        Assert.Equal(ApiFactory.AdminUserId(factory), reviewed.ReviewedBy);
        Assert.NotNull(reviewed.ReviewedAt);

        var again = await admin.PostAsync($"/api/v1/admin/submissions/{submission.Id}/approve", null);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal("Already reviewed.", await TitleAsync(again));

        // Rejecting after approval is refused too, and the points stay as they were.
        Assert.Equal(HttpStatusCode.Conflict, (await RejectAsync(admin, submission.Id, "Changed my mind")).StatusCode);

        var points = (await member.GetFromJsonAsync<PointsHistoryResponse>("/api/v1/me/points", Json))!;
        Assert.Equal(10, points.Balance);
        var earn = Assert.Single(points.Items);
        Assert.Equal((10, PointsEntryType.Earn, PointsSourceType.Submission, submission.Id),
            (earn.Amount, earn.Type, earn.SourceType, earn.SourceId));

        Assert.Equal(1, await AuditCountAsync(admin, submission.Id));
    }

    [Fact]
    public async Task Rejecting_needs_a_reason_and_writes_no_points()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var (member, _) = await TestAuth.VerifiedMemberAsync(factory);
        var submission = await ReadAsync(await SubmitAsync(member, Recycling));

        Assert.Equal(HttpStatusCode.BadRequest, (await RejectAsync(admin, submission.Id, "  ")).StatusCode);

        var rejected = await RejectAsync(admin, submission.Id, " Photo doesn't show the action ");

        Assert.Equal(HttpStatusCode.OK, rejected.StatusCode);
        var reviewed = (await rejected.Content.ReadFromJsonAsync<SubmissionResponse>(Json))!;
        Assert.Equal((SubmissionStatus.Rejected, "Photo doesn't show the action"), (reviewed.Status, reviewed.RejectReason));

        var points = (await member.GetFromJsonAsync<PointsHistoryResponse>("/api/v1/me/points", Json))!;
        Assert.Equal(0, points.Balance);
        Assert.Empty(points.Items);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsync($"/api/v1/admin/submissions/{submission.Id}/approve", null)).StatusCode);
        Assert.Equal(1, await AuditCountAsync(admin, submission.Id));
    }

    [Fact]
    public async Task The_queue_lists_pending_submissions_with_the_member_name()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var (member, _) = await TestAuth.VerifiedMemberAsync(factory);
        var submission = await ReadAsync(await SubmitAsync(member, Recycling));

        var queue = await admin.GetFromJsonAsync<PagedResult<SubmissionResponse>>(
            "/api/v1/admin/submissions?status=Pending&pageSize=100", Json);
        var detail = await admin.GetFromJsonAsync<SubmissionResponse>($"/api/v1/admin/submissions/{submission.Id}", Json);

        Assert.All(queue!.Items, s => Assert.Equal(SubmissionStatus.Pending, s.Status));
        Assert.Equal("Test Buyer", detail!.MemberName);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/v1/admin/submissions/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task Members_cannot_use_the_moderation_endpoints()
    {
        var (member, _) = await TestAuth.VerifiedMemberAsync(factory);
        var submission = await ReadAsync(await SubmitAsync(member, Recycling));

        Assert.Equal(HttpStatusCode.Forbidden, (await member.GetAsync("/api/v1/admin/submissions")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.PostAsync($"/api/v1/admin/submissions/{submission.Id}/approve", null)).StatusCode);
    }

    private static Task<HttpResponseMessage> SubmitAsync(
        HttpClient client, Guid categoryId, string? note = null, decimal? weightKg = null, byte[]? photo = null)
    {
        var file = new ByteArrayContent(photo ?? Png);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        var form = new MultipartFormDataContent
        {
            { file, "photo", "action.png" },
            { new StringContent(categoryId.ToString()), "categoryId" },
        };
        if (note is not null)
        {
            form.Add(new StringContent(note), "note");
        }

        if (weightKg is not null)
        {
            form.Add(new StringContent(weightKg.Value.ToString(CultureInfo.InvariantCulture)), "weightKg");
        }

        return client.PostAsync("/api/v1/submissions", form);
    }

    private static Task<HttpResponseMessage> RejectAsync(HttpClient admin, Guid submissionId, string reason) =>
        admin.PostAsJsonAsync($"/api/v1/admin/submissions/{submissionId}/reject", new RejectSubmissionRequest(reason));

    private static async Task<SubmissionResponse> ReadAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<SubmissionResponse>(Json))!;
    }

    private static async Task SetDailyLimitAsync(HttpClient admin, int limit) =>
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync("/api/v1/admin/settings", new UpdateSettingsRequest(limit))).StatusCode);

    private static async Task<int> AuditCountAsync(HttpClient admin, Guid submissionId) =>
        (await admin.GetFromJsonAsync<PagedResult<AuditLogResponse>>(
            $"/api/v1/admin/audit-logs?entity=submission&entityId={submissionId}"))!.Total;

    private static async Task<string?> TitleAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("title").GetString();
}
