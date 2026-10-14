using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Domain.Enums;
using RowCycle.Tests.Infrastructure;
using static RowCycle.Tests.Infrastructure.TestActions;
using static RowCycle.Tests.Infrastructure.TestShop;

namespace RowCycle.Tests;

/// <summary>
/// FR-12: badges are awarded after approvals and deliveries, each once. Badges are global, so every test makes its own
/// (unique name) and only checks for that badge, never for a total count.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class BadgeTests(ApiFactory factory)
{
    private static readonly JsonSerializerOptions Json = TestJson.Options;

    private readonly string _tag = $"b{Guid.NewGuid():N}"[..10];

    [Fact]
    public async Task Total_approved_badge_is_awarded_when_the_threshold_is_reached_and_only_once()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var badge = await CreateBadgeAsync(admin, $"{_tag} Two actions", BadgeRuleType.TotalApproved, threshold: 2);
        var (member, _) = await TestAuth.VerifiedMemberAsync(factory);

        await SubmitAndApproveAsync(member, admin, Recycling);
        Assert.DoesNotContain(await MyBadgesAsync(member), b => b.Id == badge.Id);

        await SubmitAndApproveAsync(member, admin, CanCollection);
        var earned = Assert.Single(await MyBadgesAsync(member), b => b.Id == badge.Id);
        Assert.Equal(("star", $"{_tag} Two actions"), (earned.Icon, earned.Name));

        await SubmitAndApproveAsync(member, admin, Recycling);
        Assert.Single(await MyBadgesAsync(member), b => b.Id == badge.Id);
    }

    [Fact]
    public async Task Category_badge_counts_only_approvals_in_its_category()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var badge = await CreateBadgeAsync(admin, $"{_tag} Can collector", BadgeRuleType.CategoryApproved, threshold: 1, categoryId: CanCollection);
        var (member, _) = await TestAuth.VerifiedMemberAsync(factory);

        await SubmitAndApproveAsync(member, admin, Recycling);
        Assert.DoesNotContain(await MyBadgesAsync(member), b => b.Id == badge.Id);

        // A rejected can collection doesn't count either.
        var rejected = await SubmitAsync(member, CanCollection);
        await admin.PostAsJsonAsync($"/api/v1/admin/submissions/{rejected}/reject", new RejectSubmissionRequest("Blurry"));
        await SubmitAndApproveAsync(member, admin, Recycling);
        Assert.DoesNotContain(await MyBadgesAsync(member), b => b.Id == badge.Id);

        await SubmitAndApproveAsync(member, admin, CanCollection);
        await SubmitAndApproveAsync(member, admin, CanCollection);
        Assert.Single(await MyBadgesAsync(member), b => b.Id == badge.Id);
    }

    [Theory]
    [InlineData(PaymentMethod.Cod)]
    [InlineData(PaymentMethod.Points)]
    public async Task First_order_badge_is_awarded_on_delivery_not_when_placed(PaymentMethod method)
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var badge = await CreateBadgeAsync(admin, $"{_tag} First purchase", BadgeRuleType.FirstOrder, threshold: 0);
        Assert.Equal(1, badge.Threshold);   // ignored for this rule and stored as 1
        var product = await CreateProductAsync(admin, $"{_tag} Pin", 20m, stock: 10, pricePoints: 50);
        var (member, userId) = await TestAuth.VerifiedMemberAsync(factory);
        await GivePointsAsync(factory, userId, 500);
        var addressId = await AddAddressAsync(member);

        var first = await PlaceOrderAsync(member, product, addressId, method);
        Assert.DoesNotContain(await MyBadgesAsync(member), b => b.Id == badge.Id);

        await DeliverAsync(admin, first);
        Assert.Single(await MyBadgesAsync(member), b => b.Id == badge.Id);

        var second = await PlaceOrderAsync(member, product, addressId, method);
        await DeliverAsync(admin, second);
        Assert.Single(await MyBadgesAsync(member), b => b.Id == badge.Id);
    }

    [Fact]
    public async Task A_cancelled_order_earns_no_first_order_badge()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var badge = await CreateBadgeAsync(admin, $"{_tag} First purchase", BadgeRuleType.FirstOrder, threshold: 1);
        var product = await CreateProductAsync(admin, $"{_tag} Pin", 20m, stock: 10);
        var (member, _) = await TestAuth.VerifiedMemberAsync(factory);
        var addressId = await AddAddressAsync(member);

        var order = await PlaceOrderAsync(member, product, addressId, PaymentMethod.Cod);
        Assert.Equal(HttpStatusCode.OK, (await member.PostAsync($"/api/v1/me/orders/{order}/cancel", null)).StatusCode);

        Assert.DoesNotContain(await MyBadgesAsync(member), b => b.Id == badge.Id);
    }

    [Fact]
    public async Task An_earned_badge_stays_after_the_admin_deletes_it_and_is_no_longer_awarded()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var badge = await CreateBadgeAsync(admin, $"{_tag} Starter", BadgeRuleType.TotalApproved, threshold: 1);
        var (member, _) = await TestAuth.VerifiedMemberAsync(factory);
        await SubmitAndApproveAsync(member, admin, Recycling);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/admin/badges/{badge.Id}")).StatusCode);

        Assert.Single(await MyBadgesAsync(member), b => b.Id == badge.Id);
        Assert.DoesNotContain(await AdminBadgesAsync(admin), b => b.Id == badge.Id);
        var (other, _) = await TestAuth.VerifiedMemberAsync(factory);
        await SubmitAndApproveAsync(other, admin, Recycling);
        Assert.DoesNotContain(await MyBadgesAsync(other), b => b.Id == badge.Id);

        // The name is free again once the badge is deleted.
        await CreateBadgeAsync(admin, $"{_tag} Starter", BadgeRuleType.TotalApproved, threshold: 1);
    }

    [Fact]
    public async Task Badge_rules_are_validated()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var created = await CreateBadgeAsync(admin, $"{_tag} Unique", BadgeRuleType.TotalApproved, threshold: 3);

        Assert.Equal(HttpStatusCode.BadRequest, (await PostBadgeAsync(admin,
            new SaveBadgeRequest($"{_tag} Bad icon", "rocket_launch", BadgeRuleType.TotalApproved, null, 1))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await PostBadgeAsync(admin,
            new SaveBadgeRequest($"{_tag} No category", "star", BadgeRuleType.CategoryApproved, null, 1))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await PostBadgeAsync(admin,
            new SaveBadgeRequest($"{_tag} Unknown category", "star", BadgeRuleType.CategoryApproved, Guid.NewGuid(), 1))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await PostBadgeAsync(admin,
            new SaveBadgeRequest($"{_tag} Zero", "star", BadgeRuleType.TotalApproved, null, 0))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await PostBadgeAsync(admin,
            new SaveBadgeRequest(created.Name.ToUpperInvariant(), "star", BadgeRuleType.TotalApproved, null, 1))).StatusCode);

        var icons = await admin.GetFromJsonAsync<List<string>>("/api/v1/admin/badges/icons");
        Assert.Contains("emoji_events", icons!);
    }

    [Fact]
    public async Task Badge_changes_are_admin_only_and_audit_logged()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var (member, _) = await TestAuth.VerifiedMemberAsync(factory);
        var badge = await CreateBadgeAsync(admin, $"{_tag} Logged", BadgeRuleType.TotalApproved, threshold: 1);

        var updated = await admin.PutAsJsonAsync($"/api/v1/admin/badges/{badge.Id}",
            new SaveBadgeRequest($"{_tag} Logged", "eco", BadgeRuleType.CategoryApproved, CanCollection, 5), Json);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var after = (await updated.Content.ReadFromJsonAsync<BadgeResponse>(Json))!;
        Assert.Equal(("eco", "Can collection", 5), (after.Icon, after.RuleCategoryName, after.Threshold));

        Assert.Equal(HttpStatusCode.Forbidden, (await member.GetAsync("/api/v1/admin/badges")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await PostBadgeAsync(member,
            new SaveBadgeRequest($"{_tag} Mine", "star", BadgeRuleType.TotalApproved, null, 1))).StatusCode);

        var log = await admin.GetFromJsonAsync<PagedResult<AuditLogResponse>>($"/api/v1/admin/audit-logs?entity=badge&entityId={badge.Id}");
        Assert.Equal(["badge.create", "badge.update"], log!.Items.Select(e => e.Action).Order());
    }

    // ---------- Submission categories editor ----------

    [Fact]
    public async Task Admin_can_add_and_edit_action_categories_and_members_see_them()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var (member, _) = await TestAuth.VerifiedMemberAsync(factory);

        var created = await admin.PostAsJsonAsync("/api/v1/admin/submission-categories",
            new SaveSubmissionCategoryRequest($"{_tag} Tree planting", 25, false, true));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var category = (await created.Content.ReadFromJsonAsync<AdminSubmissionCategoryResponse>())!;
        Assert.Contains(await MemberCategoriesAsync(member), c => c.Id == category.Id && c.PointsReward == 25);

        // Approval pays the points the category has at that moment.
        var updated = await admin.PutAsJsonAsync($"/api/v1/admin/submission-categories/{category.Id}",
            new SaveSubmissionCategoryRequest($"{_tag} Tree planting", 40, true, true));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        await SubmitAndApproveAsync(member, admin, category.Id);
        Assert.Equal(40, (await MeAsync(member)).PointsBalance);

        // Inactive: gone from the member's list, still in the admin's.
        await admin.PutAsJsonAsync($"/api/v1/admin/submission-categories/{category.Id}",
            new SaveSubmissionCategoryRequest($"{_tag} Tree planting", 40, true, false));
        Assert.DoesNotContain(await MemberCategoriesAsync(member), c => c.Id == category.Id);
        var all = await admin.GetFromJsonAsync<List<AdminSubmissionCategoryResponse>>("/api/v1/admin/submission-categories");
        Assert.Contains(all!, c => c.Id == category.Id && !c.IsActive);

        var duplicate = await admin.PostAsJsonAsync("/api/v1/admin/submission-categories",
            new SaveSubmissionCategoryRequest("recycling", 5, false, true));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.GetAsync("/api/v1/admin/submission-categories")).StatusCode);
    }

    [Fact]
    public async Task A_category_used_by_a_badge_cannot_be_deleted()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var created = await admin.PostAsJsonAsync("/api/v1/admin/submission-categories",
            new SaveSubmissionCategoryRequest($"{_tag} Beach", 10, true, true));
        var category = (await created.Content.ReadFromJsonAsync<AdminSubmissionCategoryResponse>())!;
        var badge = await CreateBadgeAsync(admin, $"{_tag} Beach lover", BadgeRuleType.CategoryApproved, threshold: 3, categoryId: category.Id);

        Assert.Equal(HttpStatusCode.Conflict, (await admin.DeleteAsync($"/api/v1/admin/submission-categories/{category.Id}")).StatusCode);

        await admin.DeleteAsync($"/api/v1/admin/badges/{badge.Id}");
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/admin/submission-categories/{category.Id}")).StatusCode);
        var all = await admin.GetFromJsonAsync<List<AdminSubmissionCategoryResponse>>("/api/v1/admin/submission-categories");
        Assert.DoesNotContain(all!, c => c.Id == category.Id);
    }

    // ---------- Helpers ----------

    private static async Task<BadgeResponse> CreateBadgeAsync(
        HttpClient admin, string name, BadgeRuleType rule, int threshold, Guid? categoryId = null)
    {
        var response = await PostBadgeAsync(admin, new SaveBadgeRequest(name, "star", rule, categoryId, threshold));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<BadgeResponse>(Json))!;
    }

    private static Task<HttpResponseMessage> PostBadgeAsync(HttpClient client, SaveBadgeRequest request) =>
        client.PostAsJsonAsync("/api/v1/admin/badges", request, Json);

    private static async Task<List<MyBadgeResponse>> MyBadgesAsync(HttpClient member) =>
        (await member.GetFromJsonAsync<List<MyBadgeResponse>>("/api/v1/me/badges", Json))!;

    private static async Task<List<BadgeResponse>> AdminBadgesAsync(HttpClient admin) =>
        (await admin.GetFromJsonAsync<List<BadgeResponse>>("/api/v1/admin/badges", Json))!;

    private static async Task<List<SubmissionCategoryResponse>> MemberCategoriesAsync(HttpClient member) =>
        (await member.GetFromJsonAsync<List<SubmissionCategoryResponse>>("/api/v1/submission-categories"))!;

    private static async Task<Guid> PlaceOrderAsync(HttpClient member, AdminProductResponse product, Guid addressId, PaymentMethod method)
    {
        await AddToCartAsync(member, product, 1);
        var response = await member.PostAsJsonAsync("/api/v1/orders", new CheckoutRequest(addressId, method), Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<OrderResponse>(Json))!.Id;
    }

    private static async Task DeliverAsync(HttpClient admin, Guid orderId)
    {
        foreach (var status in new[] { OrderStatus.Confirmed, OrderStatus.Shipped, OrderStatus.Delivered })
        {
            var response = await admin.PutAsJsonAsync($"/api/v1/admin/orders/{orderId}/status", new ChangeOrderStatusRequest(status), Json);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
