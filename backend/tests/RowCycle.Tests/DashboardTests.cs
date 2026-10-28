using System.Net;
using System.Net.Http.Json;
using RowCycle.Application.Dtos;
using RowCycle.Domain.Enums;
using RowCycle.Tests.Infrastructure;
using static RowCycle.Tests.Infrastructure.TestShop;

namespace RowCycle.Tests;

/// <summary>
/// The staff dashboard (F8). Tests in the collection run one at a time, so before/after differences in the global
/// counts come from the test itself.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class DashboardTests(ApiFactory factory)
{
    private readonly string _tag = $"d{Guid.NewGuid():N}"[..10];

    [Fact]
    public async Task Counts_follow_new_submissions_and_orders()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var before = await DashboardAsync(admin);

        var (member, _) = await TestAuth.VerifiedMemberAsync(factory);
        await TestActions.SubmitAsync(member, TestActions.Recycling);
        var product = await CreateProductAsync(admin, $"{_tag} Pin", 20m, stock: 50);
        await AddToCartAsync(member, product, 1);
        var addressId = await AddAddressAsync(member);
        var order = await member.PostAsJsonAsync("/api/v1/orders", new CheckoutRequest(addressId, PaymentMethod.Cod), TestJson.Options);
        Assert.Equal(HttpStatusCode.Created, order.StatusCode);

        var after = await DashboardAsync(admin);
        Assert.Equal(before.PendingSubmissions + 1, after.PendingSubmissions);
        Assert.Equal(before.NewOrders + 1, after.NewOrders);
    }

    [Fact]
    public async Task Low_stock_counts_active_products_at_or_below_the_setting()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var settings = (await admin.GetFromJsonAsync<SettingsResponse>("/api/v1/admin/settings"))!;
        Assert.Equal(5, settings.LowStockThreshold);
        var before = (await DashboardAsync(admin)).LowStockCount;

        await CreateProductAsync(admin, $"{_tag} At threshold", 10m, stock: 5);
        await CreateProductAsync(admin, $"{_tag} Plenty", 10m, stock: 6);
        var inactive = await CreateProductAsync(admin, $"{_tag} Inactive", 10m, stock: 1);
        await admin.PutAsJsonAsync($"/api/v1/admin/products/{inactive.Id}",
            new SaveProductRequest(inactive.CategoryId, inactive.Name, inactive.Slug, inactive.Description, 10m, null, 0, false));

        var after = await DashboardAsync(admin);
        Assert.Equal(before + 1, after.LowStockCount);
        Assert.Equal(5, after.LowStockThreshold);
        Assert.True(after.LowStock.Count <= 20);
        Assert.Equal(after.LowStock.OrderBy(i => i.Stock).Select(i => i.Stock), after.LowStock.Select(i => i.Stock));

        // Raising the threshold to 6 also counts the "Plenty" product.
        await admin.PutAsJsonAsync("/api/v1/admin/settings", new UpdateSettingsRequest(settings.DailySubmissionLimit, 6));
        try
        {
            Assert.Equal(before + 2, (await DashboardAsync(admin)).LowStockCount);
        }
        finally
        {
            await admin.PutAsJsonAsync("/api/v1/admin/settings", new UpdateSettingsRequest(settings.DailySubmissionLimit, 5));
        }
    }

    [Fact]
    public async Task Members_cannot_see_the_dashboard()
    {
        var (member, _) = await TestAuth.VerifiedMemberAsync(factory);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.GetAsync("/api/v1/admin/dashboard")).StatusCode);
    }

    private static async Task<DashboardResponse> DashboardAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<DashboardResponse>("/api/v1/admin/dashboard"))!;
}
