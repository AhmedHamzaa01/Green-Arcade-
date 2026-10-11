using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using RowCycle.Application.Audit;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Application.Points;
using RowCycle.Domain.Constants;
using RowCycle.Domain.Enums;
using RowCycle.Tests.Infrastructure;

namespace RowCycle.Tests;

[Collection(ApiCollection.Name)]
public sealed class OrderTests(ApiFactory factory)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _tag = $"o{Guid.NewGuid():N}"[..10];

    [Fact]
    public async Task Cart_operations_work_end_to_end()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var product = await CreateProductAsync(admin, $"{_tag} Cart Item", 25m, stock: 10);
        var variantId = product.Variants[0].Id;

        var (member, _) = await TestAuth.VerifiedMemberClientAsync(factory);

        // 1. Initial cart is empty
        var emptyCart = await member.GetFromJsonAsync<CartResponse>("/api/v1/cart", JsonOptions);
        Assert.NotNull(emptyCart);
        Assert.Empty(emptyCart.Items);
        Assert.Equal(0, emptyCart.TotalEgp);

        // 2. Add item to cart
        var addResponse = await member.PostAsJsonAsync("/api/v1/cart/items", new AddToCartRequest(variantId, 2));
        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);
        var cartAfterAdd = await addResponse.Content.ReadFromJsonAsync<CartResponse>(JsonOptions);
        Assert.NotNull(cartAfterAdd);
        var item = Assert.Single(cartAfterAdd.Items);
        Assert.Equal(variantId, item.VariantId);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(50m, cartAfterAdd.TotalEgp);

        // 3. Update quantity
        var updateResponse = await member.PutAsJsonAsync($"/api/v1/cart/items/{variantId}", new UpdateCartItemRequest(5));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var cartAfterUpdate = await updateResponse.Content.ReadFromJsonAsync<CartResponse>(JsonOptions);
        Assert.NotNull(cartAfterUpdate);
        Assert.Equal(5, cartAfterUpdate.Items[0].Quantity);
        Assert.Equal(125m, cartAfterUpdate.TotalEgp);

        // 4. Remove item
        var removeResponse = await member.DeleteAsync($"/api/v1/cart/items/{variantId}");
        Assert.Equal(HttpStatusCode.OK, removeResponse.StatusCode);
        var cartAfterRemove = await removeResponse.Content.ReadFromJsonAsync<CartResponse>(JsonOptions);
        Assert.NotNull(cartAfterRemove);
        Assert.Empty(cartAfterRemove.Items);

        // 5. Add and Clear
        await member.PostAsJsonAsync("/api/v1/cart/items", new AddToCartRequest(variantId, 1));
        var clearResponse = await member.DeleteAsync("/api/v1/cart");
        Assert.Equal(HttpStatusCode.NoContent, clearResponse.StatusCode);
        var finalCart = await member.GetFromJsonAsync<CartResponse>("/api/v1/cart", JsonOptions);
        Assert.NotNull(finalCart);
        Assert.Empty(finalCart.Items);
    }

    [Fact]
    public async Task Cod_checkout_decrements_stock_snapshots_prices_and_clears_cart()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var product = await CreateProductAsync(admin, $"{_tag} COD Item", 40m, stock: 10, rewardPoints: 5);
        var variantId = product.Variants[0].Id;

        var (member, _) = await TestAuth.VerifiedMemberClientAsync(factory);
        await member.PostAsJsonAsync("/api/v1/cart/items", new AddToCartRequest(variantId, 2));

        var checkoutRequest = new CheckoutRequest(PaymentMethod.Cod, SampleAddress());
        var response = await member.PostAsJsonAsync("/api/v1/orders", checkoutRequest);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var order = await response.Content.ReadFromJsonAsync<OrderDetailResponse>(JsonOptions);
        Assert.NotNull(order);
        Assert.Equal(OrderStatus.Placed, order.Status);
        Assert.Equal(PaymentMethod.Cod, order.PaymentMethod);
        Assert.Equal(80m, order.TotalEgp);
        Assert.Equal(0, order.TotalPoints);
        Assert.Equal(10, order.TotalRewardPoints); // 2 * 5
        Assert.Single(order.Items);
        Assert.Equal(40m, order.Items[0].UnitPriceEgp);
        Assert.Equal(5, order.Items[0].UnitRewardPoints);
        Assert.Equal(2, order.Items[0].Quantity);

        // Verify stock decremented
        var updatedProduct = await admin.GetFromJsonAsync<AdminProductResponse>($"/api/v1/admin/products/{product.Id}", JsonOptions);
        Assert.Equal(8, updatedProduct!.Variants[0].Stock);

        // Verify cart is now empty
        var cart = await member.GetFromJsonAsync<CartResponse>("/api/v1/cart", JsonOptions);
        Assert.NotNull(cart);
        Assert.Empty(cart.Items);

        // Verify order appears in /me/orders
        var myOrders = await member.GetFromJsonAsync<PagedResult<OrderSummaryResponse>>("/api/v1/me/orders", JsonOptions);
        Assert.NotNull(myOrders);
        Assert.Contains(myOrders.Items, o => o.Id == order.Id);
    }

    [Fact]
    public async Task Points_checkout_deducts_points_from_balance_under_transaction()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var product = await CreateProductAsync(admin, $"{_tag} Points Item", 30m, pricePoints: 25, stock: 10, rewardPoints: 2);
        var variantId = product.Variants[0].Id;

        var (member, userId) = await TestAuth.VerifiedMemberClientAsync(factory);
        await GivePointsAsync(userId, 100);

        await member.PostAsJsonAsync("/api/v1/cart/items", new AddToCartRequest(variantId, 3));

        var checkoutRequest = new CheckoutRequest(PaymentMethod.Points, SampleAddress());
        var response = await member.PostAsJsonAsync("/api/v1/orders", checkoutRequest);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var order = await response.Content.ReadFromJsonAsync<OrderDetailResponse>(JsonOptions);
        Assert.NotNull(order);
        Assert.Equal(PaymentMethod.Points, order.PaymentMethod);
        Assert.Equal(75, order.TotalPoints); // 3 * 25
        Assert.Equal(0m, order.TotalEgp);
        Assert.Equal(6, order.TotalRewardPoints); // 3 * 2

        // Verify user balance is 100 - 75 = 25
        var me = await member.GetFromJsonAsync<MeResponse>("/api/v1/me", JsonOptions);
        Assert.Equal(25, me!.PointsBalance);

        // Verify ledger has Redeem row
        var pointsHistory = await member.GetFromJsonAsync<PointsHistoryResponse>("/api/v1/me/points", JsonOptions);
        Assert.NotNull(pointsHistory);
        Assert.Contains(pointsHistory.Items, p => p.Type == PointsEntryType.Redeem && p.Amount == -75);

        // Verify stock decremented to 7
        var updatedProduct = await admin.GetFromJsonAsync<AdminProductResponse>($"/api/v1/admin/products/{product.Id}", JsonOptions);
        Assert.Equal(7, updatedProduct!.Variants[0].Stock);
    }

    [Fact]
    public async Task Insufficient_stock_rejects_checkout_and_writes_nothing()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var product = await CreateProductAsync(admin, $"{_tag} LowStock", 10m, stock: 2);
        var variantId = product.Variants[0].Id;

        var (member, _) = await TestAuth.VerifiedMemberClientAsync(factory);
        await member.PostAsJsonAsync("/api/v1/cart/items", new AddToCartRequest(variantId, 2));

        // Admin reduces stock to 1
        await SetStockAsync(admin, product, 1);

        var response = await member.PostAsJsonAsync("/api/v1/orders", new CheckoutRequest(PaymentMethod.Cod, SampleAddress()));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        // Stock remains 1
        var updatedProduct = await admin.GetFromJsonAsync<AdminProductResponse>($"/api/v1/admin/products/{product.Id}", JsonOptions);
        Assert.Equal(1, updatedProduct!.Variants[0].Stock);
    }

    [Fact]
    public async Task Insufficient_points_rejects_checkout_and_writes_nothing()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var product = await CreateProductAsync(admin, $"{_tag} PriceyPoints", 10m, pricePoints: 50, stock: 5);
        var variantId = product.Variants[0].Id;

        var (member, userId) = await TestAuth.VerifiedMemberClientAsync(factory);
        await GivePointsAsync(userId, 30); // 30 is less than 50

        await member.PostAsJsonAsync("/api/v1/cart/items", new AddToCartRequest(variantId, 1));

        var response = await member.PostAsJsonAsync("/api/v1/orders", new CheckoutRequest(PaymentMethod.Points, SampleAddress()));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        // Balance remains 30, stock remains 5
        var me = await member.GetFromJsonAsync<MeResponse>("/api/v1/me", JsonOptions);
        Assert.Equal(30, me!.PointsBalance);
        var updatedProduct = await admin.GetFromJsonAsync<AdminProductResponse>($"/api/v1/admin/products/{product.Id}", JsonOptions);
        Assert.Equal(5, updatedProduct!.Variants[0].Stock);
    }

    [Fact]
    public async Task Unverified_member_cannot_checkout()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var product = await CreateProductAsync(admin, $"{_tag} UnverifiedCheck", 10m, stock: 5);
        var variantId = product.Variants[0].Id;

        var unverifiedMember = await TestAuth.MemberClientAsync(factory);
        await unverifiedMember.PostAsJsonAsync("/api/v1/cart/items", new AddToCartRequest(variantId, 1));

        var response = await unverifiedMember.PostAsJsonAsync("/api/v1/orders", new CheckoutRequest(PaymentMethod.Cod, SampleAddress()));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Member_can_cancel_placed_order_restoring_stock_and_refunding_points()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var product = await CreateProductAsync(admin, $"{_tag} CancelItem", 20m, pricePoints: 30, stock: 8);
        var variantId = product.Variants[0].Id;

        var (member, userId) = await TestAuth.VerifiedMemberClientAsync(factory);
        await GivePointsAsync(userId, 100);

        await member.PostAsJsonAsync("/api/v1/cart/items", new AddToCartRequest(variantId, 2)); // 60 points
        var orderResponse = await member.PostAsJsonAsync("/api/v1/orders", new CheckoutRequest(PaymentMethod.Points, SampleAddress()));
        var order = (await orderResponse.Content.ReadFromJsonAsync<OrderDetailResponse>(JsonOptions))!;

        Assert.Equal(40, (await member.GetFromJsonAsync<MeResponse>("/api/v1/me", JsonOptions))!.PointsBalance);
        Assert.Equal(6, (await admin.GetFromJsonAsync<AdminProductResponse>($"/api/v1/admin/products/{product.Id}", JsonOptions))!.Variants[0].Stock);

        // Cancel order
        var cancelResponse = await member.PostAsync($"/api/v1/me/orders/{order.Id}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);
        var cancelled = await cancelResponse.Content.ReadFromJsonAsync<OrderDetailResponse>(JsonOptions);
        Assert.Equal(OrderStatus.Cancelled, cancelled!.Status);

        // Stock restored back to 8
        Assert.Equal(8, (await admin.GetFromJsonAsync<AdminProductResponse>($"/api/v1/admin/products/{product.Id}", JsonOptions))!.Variants[0].Stock);

        // Points refunded back to 100
        Assert.Equal(100, (await member.GetFromJsonAsync<MeResponse>("/api/v1/me", JsonOptions))!.PointsBalance);

        // Points history has Reverse entry
        var history = await member.GetFromJsonAsync<PointsHistoryResponse>("/api/v1/me/points", JsonOptions);
        Assert.Contains(history!.Items, p => p.Type == PointsEntryType.Reverse && p.Amount == 60);
    }

    [Fact]
    public async Task Staff_transitions_order_and_Delivered_awards_reward_points_for_COD_and_Points()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var product = await CreateProductAsync(admin, $"{_tag} DeliverItem", 50m, pricePoints: 50, stock: 10, rewardPoints: 12);
        var variantId = product.Variants[0].Id;

        var (member, userId) = await TestAuth.VerifiedMemberClientAsync(factory);

        // 1. COD Order Delivery awards reward points
        await member.PostAsJsonAsync("/api/v1/cart/items", new AddToCartRequest(variantId, 2)); // 2 * 12 = 24 reward points
        var codOrder = (await (await member.PostAsJsonAsync("/api/v1/orders", new CheckoutRequest(PaymentMethod.Cod, SampleAddress())))
            .Content.ReadFromJsonAsync<OrderDetailResponse>(JsonOptions))!;

        Assert.Equal(0, (await member.GetFromJsonAsync<MeResponse>("/api/v1/me", JsonOptions))!.PointsBalance);

        // Placed -> Confirmed -> Shipped -> Delivered
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/v1/admin/orders/{codOrder.Id}/status", new UpdateOrderStatusRequest(OrderStatus.Confirmed))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/v1/admin/orders/{codOrder.Id}/status", new UpdateOrderStatusRequest(OrderStatus.Shipped))).StatusCode);
        var deliveredResponse = await admin.PutAsJsonAsync($"/api/v1/admin/orders/{codOrder.Id}/status", new UpdateOrderStatusRequest(OrderStatus.Delivered));
        Assert.Equal(HttpStatusCode.OK, deliveredResponse.StatusCode);

        // Buyer received 24 reward points
        Assert.Equal(24, (await member.GetFromJsonAsync<MeResponse>("/api/v1/me", JsonOptions))!.PointsBalance);

        // 2. Points Order Delivery also awards reward points
        await member.PostAsJsonAsync("/api/v1/cart/items", new AddToCartRequest(variantId, 1)); // 50 points, 12 reward points
        await GivePointsAsync(userId, 50); // total balance 24 + 50 = 74

        var pointsOrder = (await (await member.PostAsJsonAsync("/api/v1/orders", new CheckoutRequest(PaymentMethod.Points, SampleAddress())))
            .Content.ReadFromJsonAsync<OrderDetailResponse>(JsonOptions))!;

        Assert.Equal(24, (await member.GetFromJsonAsync<MeResponse>("/api/v1/me", JsonOptions))!.PointsBalance); // 74 - 50 = 24

        // Advance points order to Delivered
        await admin.PutAsJsonAsync($"/api/v1/admin/orders/{pointsOrder.Id}/status", new UpdateOrderStatusRequest(OrderStatus.Confirmed));
        await admin.PutAsJsonAsync($"/api/v1/admin/orders/{pointsOrder.Id}/status", new UpdateOrderStatusRequest(OrderStatus.Shipped));
        await admin.PutAsJsonAsync($"/api/v1/admin/orders/{pointsOrder.Id}/status", new UpdateOrderStatusRequest(OrderStatus.Delivered));

        // Buyer received 12 reward points -> 24 + 12 = 36
        Assert.Equal(36, (await member.GetFromJsonAsync<MeResponse>("/api/v1/me", JsonOptions))!.PointsBalance);
    }

    [Fact]
    public async Task Invalid_status_transitions_are_rejected()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var product = await CreateProductAsync(admin, $"{_tag} TransitionCheck", 10m, stock: 5);
        var (member, _) = await TestAuth.VerifiedMemberClientAsync(factory);

        await member.PostAsJsonAsync("/api/v1/cart/items", new AddToCartRequest(product.Variants[0].Id, 1));
        var order = (await (await member.PostAsJsonAsync("/api/v1/orders", new CheckoutRequest(PaymentMethod.Cod, SampleAddress())))
            .Content.ReadFromJsonAsync<OrderDetailResponse>(JsonOptions))!;

        // Cannot skip directly Placed -> Delivered
        var skipResponse = await admin.PutAsJsonAsync($"/api/v1/admin/orders/{order.Id}/status", new UpdateOrderStatusRequest(OrderStatus.Delivered));
        Assert.Equal(HttpStatusCode.Conflict, skipResponse.StatusCode);

        // Move to Delivered validly
        await admin.PutAsJsonAsync($"/api/v1/admin/orders/{order.Id}/status", new UpdateOrderStatusRequest(OrderStatus.Confirmed));
        await admin.PutAsJsonAsync($"/api/v1/admin/orders/{order.Id}/status", new UpdateOrderStatusRequest(OrderStatus.Shipped));
        await admin.PutAsJsonAsync($"/api/v1/admin/orders/{order.Id}/status", new UpdateOrderStatusRequest(OrderStatus.Delivered));

        // Cannot cancel an already Delivered order
        var cancelAfterDeliver = await admin.PutAsJsonAsync($"/api/v1/admin/orders/{order.Id}/status", new UpdateOrderStatusRequest(OrderStatus.Cancelled));
        Assert.Equal(HttpStatusCode.Conflict, cancelAfterDeliver.StatusCode);
    }

    [Fact]
    public async Task Admin_changing_order_status_is_audit_logged()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var product = await CreateProductAsync(admin, $"{_tag} AuditCheck", 10m, stock: 5);
        var (member, _) = await TestAuth.VerifiedMemberClientAsync(factory);

        await member.PostAsJsonAsync("/api/v1/cart/items", new AddToCartRequest(product.Variants[0].Id, 1));
        var order = (await (await member.PostAsJsonAsync("/api/v1/orders", new CheckoutRequest(PaymentMethod.Cod, SampleAddress())))
            .Content.ReadFromJsonAsync<OrderDetailResponse>(JsonOptions))!;

        await admin.PutAsJsonAsync($"/api/v1/admin/orders/{order.Id}/status", new UpdateOrderStatusRequest(OrderStatus.Confirmed));

        var logs = await admin.GetFromJsonAsync<PagedResult<AuditLogResponse>>("/api/v1/admin/audit-logs?pageSize=100", JsonOptions);
        Assert.NotNull(logs);
        Assert.Contains(logs.Items, l => l.Action == AuditActions.OrderStatusUpdate && l.EntityId == order.Id.ToString());
    }

    // ---------- helpers ----------

    private static ShippingAddressDto SampleAddress() =>
        new("Ahmed Mahmoud", "01012345678", "Alexandria", "Sidi Gaber", "El Geish Road", "Building 12", "Call upon arrival");

    private static async Task<AdminProductResponse> CreateProductAsync(
        HttpClient admin, string name, decimal price, int stock = 0, int? pricePoints = null, int rewardPoints = 0)
    {
        var categories = await admin.GetFromJsonAsync<List<ProductCategoryResponse>>("/api/v1/admin/product-categories", JsonOptions);
        var categoryId = categories!.First().Id;
        var request = new SaveProductRequest(categoryId, name, null, "Recycled item", price, pricePoints, rewardPoints, true);
        var response = await admin.PostAsJsonAsync("/api/v1/admin/products", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var product = (await response.Content.ReadFromJsonAsync<AdminProductResponse>(JsonOptions))!;

        if (stock > 0)
        {
            await SetStockAsync(admin, product, stock);
            product = (await admin.GetFromJsonAsync<AdminProductResponse>($"/api/v1/admin/products/{product.Id}", JsonOptions))!;
        }

        return product;
    }

    private static async Task SetStockAsync(HttpClient admin, AdminProductResponse product, int stock)
    {
        var variant = product.Variants[0];
        var response = await admin.PutAsJsonAsync($"/api/v1/admin/products/{product.Id}/variants/{variant.Id}",
            new SaveProductVariantRequest(variant.Name, variant.Sku, stock, variant.PriceOverride));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task GivePointsAsync(Guid userId, int amount)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var points = scope.ServiceProvider.GetRequiredService<IPointsService>();
        await points.AdjustAsync(userId, amount, "Test balance setup", ApiFactory.AdminUserId(factory));
    }
}
