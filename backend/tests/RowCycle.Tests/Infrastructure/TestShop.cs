using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RowCycle.Application.Dtos;
using RowCycle.Application.Points;

namespace RowCycle.Tests.Infrastructure;

/// <summary>Sets up products, addresses and points for checkout tests.</summary>
public static class TestShop
{
    public static async Task<AdminProductResponse> CreateProductAsync(
        HttpClient admin, string name, decimal price, int stock, int? pricePoints = null, int rewardPoints = 0)
    {
        var categories = await admin.GetFromJsonAsync<List<ProductCategoryResponse>>("/api/v1/admin/product-categories");
        var request = new SaveProductRequest(categories!.First(c => c.Slug == "medals").Id, name, null, "Test product.", price, pricePoints, rewardPoints, true);
        var created = await admin.PostAsJsonAsync("/api/v1/admin/products", request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var product = (await created.Content.ReadFromJsonAsync<AdminProductResponse>())!;
        return await SetStockAsync(admin, product, stock);
    }

    public static async Task<AdminProductResponse> SetStockAsync(HttpClient admin, AdminProductResponse product, int stock)
    {
        var variant = product.Variants[0];
        var response = await admin.PutAsJsonAsync($"/api/v1/admin/products/{product.Id}/variants/{variant.Id}",
            new SaveProductVariantRequest(variant.Name, variant.Sku, stock, variant.PriceOverride));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AdminProductResponse>())!;
    }

    public static async Task<int> StockAsync(HttpClient admin, Guid productId) =>
        (await admin.GetFromJsonAsync<AdminProductResponse>($"/api/v1/admin/products/{productId}"))!.Variants.Sum(v => v.Stock);

    public static async Task<Guid> AddAddressAsync(HttpClient member)
    {
        var response = await member.PostAsJsonAsync("/api/v1/me/addresses",
            new SaveAddressRequest("Sara Ali", "01012345678", "Alexandria", "Smouha", "Victor Emmanuel St", "12", null, true));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AddressResponse>())!.Id;
    }

    public static async Task AddToCartAsync(HttpClient member, AdminProductResponse product, int quantity)
    {
        var response = await member.PostAsJsonAsync("/api/v1/cart/items", new AddCartItemRequest(product.Variants[0].Id, quantity));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    public static async Task GivePointsAsync(ApiFactory factory, Guid userId, int points)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IPointsService>()
            .AdjustAsync(userId, points, "Test top-up", ApiFactory.AdminUserId(factory));
    }

    public static async Task<MeResponse> MeAsync(HttpClient member) => (await member.GetFromJsonAsync<MeResponse>("/api/v1/me"))!;
}
