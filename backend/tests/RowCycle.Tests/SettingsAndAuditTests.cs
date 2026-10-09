using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Application.Settings;
using RowCycle.Domain.Constants;
using RowCycle.Tests.Infrastructure;

namespace RowCycle.Tests;

[Collection(ApiCollection.Name)]
public sealed class SettingsAndAuditTests(ApiFactory factory)
{
    [Fact]
    public async Task Admin_changes_a_setting_and_the_change_is_audit_logged()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var before = (await admin.GetFromJsonAsync<SettingsResponse>("/api/v1/admin/settings"))!.DailySubmissionLimit;
        var newValue = before == 7 ? 8 : 7;

        try
        {
            var response = await admin.PutAsJsonAsync("/api/v1/admin/settings", new UpdateSettingsRequest(newValue));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(newValue, (await response.Content.ReadFromJsonAsync<SettingsResponse>())!.DailySubmissionLimit);
            Assert.Equal(newValue, (await admin.GetFromJsonAsync<SettingsResponse>("/api/v1/admin/settings"))!.DailySubmissionLimit);
            Assert.Equal(newValue, await GetLimitFromServiceAsync());

            var log = await LatestSettingsLogAsync(admin);
            Assert.Equal("settings.update", log.Action);
            Assert.Equal(ApiFactory.AdminUserId(factory), log.UserId);
            Assert.Equal(before, log.Data!.Value.GetProperty("old").GetInt32());
            Assert.Equal(newValue, log.Data!.Value.GetProperty("new").GetInt32());
        }
        finally
        {
            await admin.PutAsJsonAsync("/api/v1/admin/settings", new UpdateSettingsRequest(before));
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public async Task Invalid_value_is_rejected_and_nothing_is_logged(int value)
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var logsBefore = await CountSettingsLogsAsync(admin);

        var response = await admin.PutAsJsonAsync("/api/v1/admin/settings", new UpdateSettingsRequest(value));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty("dailySubmissionLimit", out _));
        Assert.Equal(logsBefore, await CountSettingsLogsAsync(admin));
    }

    [Fact]
    public async Task Saving_the_same_value_writes_no_audit_row()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var current = (await admin.GetFromJsonAsync<SettingsResponse>("/api/v1/admin/settings"))!.DailySubmissionLimit;
        var logsBefore = await CountSettingsLogsAsync(admin);

        var response = await admin.PutAsJsonAsync("/api/v1/admin/settings", new UpdateSettingsRequest(current));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(logsBefore, await CountSettingsLogsAsync(admin));
    }

    [Fact]
    public async Task Members_and_anonymous_users_cannot_use_admin_endpoints()
    {
        var member = await TestAuth.MemberClientAsync(factory);
        var anonymous = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Forbidden, (await member.GetAsync("/api/v1/admin/settings")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.PutAsJsonAsync("/api/v1/admin/settings", new UpdateSettingsRequest(3))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.GetAsync("/api/v1/admin/audit-logs")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/admin/settings")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/admin/audit-logs")).StatusCode);
    }

    [Fact]
    public async Task Audit_logs_are_paged_newest_first()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var before = (await admin.GetFromJsonAsync<SettingsResponse>("/api/v1/admin/settings"))!.DailySubmissionLimit;
        try
        {
            await admin.PutAsJsonAsync("/api/v1/admin/settings", new UpdateSettingsRequest(11));
            await admin.PutAsJsonAsync("/api/v1/admin/settings", new UpdateSettingsRequest(12));

            var page = await admin.GetFromJsonAsync<PagedResult<AuditLogResponse>>(
                "/api/v1/admin/audit-logs?entity=settings&pageSize=2", JsonOptions);

            Assert.Equal(2, page!.Items.Count);
            Assert.Equal(2, page.PageSize);
            Assert.True(page.Total >= 2);
            Assert.Equal(12, page.Items[0].Data!.Value.GetProperty("new").GetInt32());
            Assert.Equal(11, page.Items[1].Data!.Value.GetProperty("new").GetInt32());
            Assert.True(page.Items[0].CreatedAt >= page.Items[1].CreatedAt);
        }
        finally
        {
            await admin.PutAsJsonAsync("/api/v1/admin/settings", new UpdateSettingsRequest(before));
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private async Task<int> GetLimitFromServiceAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ISettingsService>().GetDailySubmissionLimitAsync();
    }

    private static async Task<AuditLogResponse> LatestSettingsLogAsync(HttpClient admin)
    {
        var page = await admin.GetFromJsonAsync<PagedResult<AuditLogResponse>>(
            $"/api/v1/admin/audit-logs?entity=settings&entityId={SettingKeys.DailySubmissionLimit}&pageSize=1", JsonOptions);
        return page!.Items.Single();
    }

    private static async Task<int> CountSettingsLogsAsync(HttpClient admin) =>
        (await admin.GetFromJsonAsync<PagedResult<AuditLogResponse>>("/api/v1/admin/audit-logs?entity=settings&pageSize=1", JsonOptions))!.Total;
}
