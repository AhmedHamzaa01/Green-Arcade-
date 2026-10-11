using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Domain.Enums;
using RowCycle.Infrastructure.Persistence;
using RowCycle.Tests.Infrastructure;
using static RowCycle.Tests.Infrastructure.TestShop;

namespace RowCycle.Tests;

[Collection(ApiCollection.Name)]
public sealed class OrderTests(ApiFactory factory)
{
    /// <summary>The API sends enums as text ("Placed").</summary>
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private readonly string _tag = $"o{Guid.NewGuid():N}"[..10];

    [Fact]
    public async Task Cash_on_delivery_checkout_takes_stock_copies_prices_and_empties_the_cart()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var product = await CreateProductAsync(admin, $"{_tag} Medal", 150m, stock: 5, rewardPoints: 20);
        var (member, _) = await TestAuth.VerifiedMemberAsync(factory);
        var addressId = await AddAddressAsync(member);
        await AddToCartAsync(member, product, 2);

        var response = await CheckoutAsync(member, addressId, PaymentMethod.Cod);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = (await response.Content.ReadFromJsonAsync<OrderResponse>(Json))!;
        Assert.Matches(@"^RC-\d{5,}$", order.OrderNumber);
        Assert.Equal(OrderStatus.Placed, order.Status);
        Assert.Equal(300m, order.TotalEgp);
        Assert.Equal(0, order.TotalPoints);
        Assert.Equal(40, order.TotalRewardPoints);
        Assert.True(order.RewardPending);
        Assert.Equal("Alexandria", order.ShippingAddress.City);
        var line = Assert.Single(order.Items);
        Assert.Equal((150m, 2, $"{_tag} Medal", "Default"), (line.UnitPriceEgp, line.Quantity, line.ProductName, line.VariantName));
        var placed = Assert.Single(order.StatusHistory);
        Assert.Equal((null, OrderStatus.Placed), (placed.FromStatus, placed.ToStatus));

        Assert.Equal(3, await StockAsync(admin, product.Id));
        Assert.Empty((await member.GetFromJsonAsync<CartResponse>("/api/v1/cart", Json))!.Items);

        // FR-15: a later price change doesn't touch the order.
        await admin.PutAsJsonAsync($"/api/v1/admin/products/{product.Id}",
            new SaveProductRequest(product.CategoryId, product.Name, product.Slug, product.Description, 999m, null, 0, true));
        var again = await member.GetFromJsonAsync<OrderResponse>($"/api/v1/me/orders/{order.Id}", Json);
        Assert.Equal(150m, again!.Items[0].UnitPriceEgp);
        Assert.Equal(40, again.TotalRewardPoints);
    }

    [Fact]
    public async Task Points_checkout_spends_points_and_too_few_points_changes_nothing()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var product = await CreateProductAsync(admin, $"{_tag} Pin", 50m, stock: 10, pricePoints: 200);
        var (member, userId) = await TestAuth.VerifiedMemberAsync(factory);
        var addressId = await AddAddressAsync(member);
        await AddToCartAsync(member, product, 2);
        await GivePointsAsync(factory, userId, 300);

        // 2 × 200 = 400 > 300: refused, and nothing changed.
        var refused = await CheckoutAsync(member, addressId, PaymentMethod.Points);
        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        Assert.Equal("Not enough points.", await TitleAsync(refused));
        Assert.Equal(10, await StockAsync(admin, product.Id));
        Assert.Equal(300, (await MeAsync(member)).PointsBalance);
        Assert.Equal(2, (await member.GetFromJsonAsync<CartResponse>("/api/v1/cart", Json))!.ItemCount);
        Assert.Equal(0, (await member.GetFromJsonAsync<PagedResult<OrderSummaryResponse>>("/api/v1/me/orders", Json))!.Total);

        await GivePointsAsync(factory, userId, 100);
        var ok = await CheckoutAsync(member, addressId, PaymentMethod.Points);

        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
        Assert.Equal(400, (await ok.Content.ReadFromJsonAsync<OrderResponse>(Json))!.TotalPoints);
        Assert.Equal(0, (await MeAsync(member)).PointsBalance);
        Assert.Equal(8, await StockAsync(admin, product.Id));
    }

    [Fact]
    public async Task Points_checkout_needs_a_points_price_on_every_item()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var withPoints = await CreateProductAsync(admin, $"{_tag} A", 10m, stock: 5, pricePoints: 10);
        var cashOnly = await CreateProductAsync(admin, $"{_tag} B", 10m, stock: 5);
        var (member, userId) = await TestAuth.VerifiedMemberAsync(factory);
        var addressId = await AddAddressAsync(member);
        await GivePointsAsync(factory, userId, 1000);
        await AddToCartAsync(member, withPoints, 1);
        await AddToCartAsync(member, cashOnly, 1);

        var cart = await member.GetFromJsonAsync<CartResponse>("/api/v1/cart", Json);
        Assert.False(cart!.CanPayWithPoints);
        Assert.Null(cart.TotalPoints);

        var response = await CheckoutAsync(member, addressId, PaymentMethod.Points);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(1000, (await MeAsync(member)).PointsBalance);
    }

    [Fact]
    public async Task Not_enough_stock_refuses_the_whole_checkout()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var a = await CreateProductAsync(admin, $"{_tag} Plenty", 10m, stock: 10);
        var b = await CreateProductAsync(admin, $"{_tag} Scarce", 10m, stock: 3);
        var (member, _) = await TestAuth.VerifiedMemberAsync(factory);
        var addressId = await AddAddressAsync(member);
        await AddToCartAsync(member, a, 2);
        await AddToCartAsync(member, b, 3);
        await SetStockAsync(admin, b, 1);   // stock dropped after it went in the cart

        Assert.True((await member.GetFromJsonAsync<CartResponse>("/api/v1/cart", Json))!.HasIssues);
        var response = await CheckoutAsync(member, addressId, PaymentMethod.Cod);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Not enough stock.", await TitleAsync(response));
        Assert.Equal(10, await StockAsync(admin, a.Id));   // the other line wasn't taken either
        Assert.Equal(1, await StockAsync(admin, b.Id));
    }

    [Fact]
    public async Task Two_buyers_cannot_both_get_the_last_item()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var product = await CreateProductAsync(admin, $"{_tag} Last", 10m, stock: 1);
        var buyers = new List<(HttpClient Client, Guid AddressId)>();
        for (var i = 0; i < 4; i++)
        {
            var (member, _) = await TestAuth.VerifiedMemberAsync(factory);
            await AddToCartAsync(member, product, 1);
            buyers.Add((member, await AddAddressAsync(member)));
        }

        var results = await Task.WhenAll(buyers.Select(b => Task.Run(() => CheckoutAsync(b.Client, b.AddressId, PaymentMethod.Cod))));

        Assert.Equal(1, results.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(3, results.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        Assert.Equal(0, await StockAsync(admin, product.Id));
    }

    [Fact]
    public async Task Cancelling_returns_stock_and_points_exactly_once()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var product = await CreateProductAsync(admin, $"{_tag} Refund", 10m, stock: 5, pricePoints: 100);
        var (member, userId) = await TestAuth.VerifiedMemberAsync(factory);
        var addressId = await AddAddressAsync(member);
        await GivePointsAsync(factory, userId, 250);
        await AddToCartAsync(member, product, 2);
        var order = await PlaceAsync(member, addressId, PaymentMethod.Points);
        Assert.Equal(50, (await MeAsync(member)).PointsBalance);

        var cancel = await member.PostAsync($"/api/v1/me/orders/{order.Id}/cancel", null);

        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        var cancelled = (await cancel.Content.ReadFromJsonAsync<OrderResponse>(Json))!;
        Assert.Equal(OrderStatus.Cancelled, cancelled.Status);
        Assert.False(cancelled.RewardPending);
        Assert.Equal(250, (await MeAsync(member)).PointsBalance);
        Assert.Equal(5, await StockAsync(admin, product.Id));

        // A second cancel is refused and gives nothing back twice.
        Assert.Equal(HttpStatusCode.Conflict, (await member.PostAsync($"/api/v1/me/orders/{order.Id}/cancel", null)).StatusCode);
        Assert.Equal(250, (await MeAsync(member)).PointsBalance);
        Assert.Equal(5, await StockAsync(admin, product.Id));
    }

    [Fact]
    public async Task Members_cancel_only_placed_orders_but_staff_can_cancel_confirmed_ones()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var product = await CreateProductAsync(admin, $"{_tag} Confirmed", 10m, stock: 5);
        var (member, _) = await TestAuth.VerifiedMemberAsync(factory);
        var addressId = await AddAddressAsync(member);
        await AddToCartAsync(member, product, 1);
        var order = await PlaceAsync(member, addressId, PaymentMethod.Cod);
        await ChangeStatusAsync(admin, order.Id, OrderStatus.Confirmed, HttpStatusCode.OK);

        Assert.Equal(HttpStatusCode.Conflict, (await member.PostAsync($"/api/v1/me/orders/{order.Id}/cancel", null)).StatusCode);

        var cancelled = await ChangeStatusAsync(admin, order.Id, OrderStatus.Cancelled, HttpStatusCode.OK);
        Assert.Equal(OrderStatus.Cancelled, cancelled!.Status);
        Assert.Equal(5, await StockAsync(admin, product.Id));
    }

    [Theory]
    [InlineData(PaymentMethod.Cod)]
    [InlineData(PaymentMethod.Points)]
    public async Task Delivered_pays_reward_points_once_for_both_payment_methods(PaymentMethod method)
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var product = await CreateProductAsync(admin, $"{_tag} Reward", 100m, stock: 5, pricePoints: 50, rewardPoints: 15);
        var (member, userId) = await TestAuth.VerifiedMemberAsync(factory);
        var addressId = await AddAddressAsync(member);
        await GivePointsAsync(factory, userId, 100);
        await AddToCartAsync(member, product, 2);
        var order = await PlaceAsync(member, addressId, method);
        var balanceAfterCheckout = (await MeAsync(member)).PointsBalance;

        await ChangeStatusAsync(admin, order.Id, OrderStatus.Confirmed, HttpStatusCode.OK);
        await ChangeStatusAsync(admin, order.Id, OrderStatus.Shipped, HttpStatusCode.OK);
        Assert.Equal(balanceAfterCheckout, (await MeAsync(member)).PointsBalance);   // nothing until delivery

        var delivered = await ChangeStatusAsync(admin, order.Id, OrderStatus.Delivered, HttpStatusCode.OK);

        Assert.False(delivered!.RewardPending);
        Assert.Empty(delivered.NextStatuses);
        Assert.Equal(balanceAfterCheckout + 30, (await MeAsync(member)).PointsBalance);
        await ChangeStatusAsync(admin, order.Id, OrderStatus.Delivered, HttpStatusCode.Conflict);
        Assert.Equal(1, await CountLedgerAsync(order.Id, PointsEntryType.Earn));

        var history = (await member.GetFromJsonAsync<OrderResponse>($"/api/v1/me/orders/{order.Id}", Json))!.StatusHistory;
        Assert.Equal(
            [OrderStatus.Placed, OrderStatus.Confirmed, OrderStatus.Shipped, OrderStatus.Delivered],
            history.Select(h => h.ToStatus));
    }

    [Fact]
    public async Task Invalid_status_moves_are_refused()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var product = await CreateProductAsync(admin, $"{_tag} Flow", 10m, stock: 5);
        var (member, _) = await TestAuth.VerifiedMemberAsync(factory);
        var addressId = await AddAddressAsync(member);
        await AddToCartAsync(member, product, 1);
        var order = await PlaceAsync(member, addressId, PaymentMethod.Cod);

        await ChangeStatusAsync(admin, order.Id, OrderStatus.Delivered, HttpStatusCode.Conflict);
        await ChangeStatusAsync(admin, order.Id, OrderStatus.Shipped, HttpStatusCode.Conflict);
        await ChangeStatusAsync(admin, order.Id, OrderStatus.Confirmed, HttpStatusCode.OK);
        await ChangeStatusAsync(admin, order.Id, OrderStatus.Shipped, HttpStatusCode.OK);
        await ChangeStatusAsync(admin, order.Id, OrderStatus.Cancelled, HttpStatusCode.Conflict);   // too late to cancel
        await ChangeStatusAsync(admin, order.Id, OrderStatus.Placed, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Status_changes_are_audit_logged()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var product = await CreateProductAsync(admin, $"{_tag} Audit", 10m, stock: 5);
        var (member, _) = await TestAuth.VerifiedMemberAsync(factory);
        await AddToCartAsync(member, product, 1);
        var order = await PlaceAsync(member, await AddAddressAsync(member), PaymentMethod.Cod);

        await ChangeStatusAsync(admin, order.Id, OrderStatus.Confirmed, HttpStatusCode.OK);

        var logs = await admin.GetFromJsonAsync<PagedResult<AuditLogResponse>>($"/api/v1/admin/audit-logs?entity=order", Json);
        var log = Assert.Single(logs!.Items, l => l.EntityId == order.Id.ToString());
        Assert.Equal("order.status", log.Action);
        Assert.Equal("Confirmed", log.Data!.Value.GetProperty("to").ToString());
    }

    [Fact]
    public async Task Members_only_see_their_own_orders_and_staff_endpoints_are_protected()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var product = await CreateProductAsync(admin, $"{_tag} Mine", 10m, stock: 5);
        var (owner, _) = await TestAuth.VerifiedMemberAsync(factory);
        await AddToCartAsync(owner, product, 1);
        var order = await PlaceAsync(owner, await AddAddressAsync(owner), PaymentMethod.Cod);
        var (stranger, _) = await TestAuth.VerifiedMemberAsync(factory);

        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/api/v1/me/orders/{order.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.PostAsync($"/api/v1/me/orders/{order.Id}/cancel", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync("/api/v1/admin/orders")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.PutAsJsonAsync($"/api/v1/admin/orders/{order.Id}/status",
            new ChangeOrderStatusRequest(OrderStatus.Confirmed), Json)).StatusCode);

        var staffList = await admin.GetFromJsonAsync<PagedResult<OrderSummaryResponse>>($"/api/v1/admin/orders?search={order.OrderNumber}", Json);
        Assert.Equal(order.Id, Assert.Single(staffList!.Items).Id);
    }

    [Fact]
    public async Task Unverified_members_cannot_check_out()
    {
        var member = await TestAuth.MemberClientAsync(factory);

        var response = await member.PostAsJsonAsync("/api/v1/orders", new CheckoutRequest(Guid.NewGuid(), PaymentMethod.Cod), Json);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Checkout_needs_items_and_one_of_your_own_addresses()
    {
        var (member, _) = await TestAuth.VerifiedMemberAsync(factory);
        var (other, _) = await TestAuth.VerifiedMemberAsync(factory);
        var addressId = await AddAddressAsync(member);
        var someoneElsesAddress = await AddAddressAsync(other);

        Assert.Equal("Your cart is empty.", await TitleAsync(await CheckoutAsync(member, addressId, PaymentMethod.Cod)));

        var admin = await TestAuth.AdminClientAsync(factory);
        await AddToCartAsync(member, await CreateProductAsync(admin, $"{_tag} Addr", 10m, stock: 5), 1);
        var response = await CheckoutAsync(member, someoneElsesAddress, PaymentMethod.Cod);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Address not found.", await TitleAsync(response));
    }

    [Fact]
    public async Task Cart_checks_stock_and_flags_products_that_left_the_store()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var product = await CreateProductAsync(admin, $"{_tag} Cart", 25m, stock: 3);
        var soldOut = await CreateProductAsync(admin, $"{_tag} Sold out", 25m, stock: 0);
        var (member, _) = await TestAuth.VerifiedMemberAsync(factory);

        Assert.Equal(HttpStatusCode.Conflict, (await member.PostAsJsonAsync("/api/v1/cart/items", new AddCartItemRequest(soldOut.Variants[0].Id, 1))).StatusCode);
        await AddToCartAsync(member, product, 2);
        Assert.Equal(HttpStatusCode.Conflict, (await member.PostAsJsonAsync("/api/v1/cart/items", new AddCartItemRequest(product.Variants[0].Id, 2))).StatusCode);
        await AddToCartAsync(member, product, 1);   // 2 + 1 = 3 is fine

        var cart = await member.GetFromJsonAsync<CartResponse>("/api/v1/cart", Json);
        Assert.Equal((3, 75m), (cart!.ItemCount, cart.TotalEgp));

        var lowered = await member.PutAsJsonAsync($"/api/v1/cart/items/{product.Variants[0].Id}", new UpdateCartItemRequest(1));
        Assert.Equal(25m, (await lowered.Content.ReadFromJsonAsync<CartResponse>(Json))!.TotalEgp);

        // Deactivated while in the cart: still listed, but flagged.
        await admin.PutAsJsonAsync($"/api/v1/admin/products/{product.Id}",
            new SaveProductRequest(product.CategoryId, product.Name, product.Slug, product.Description, 25m, null, 0, IsActive: false));
        cart = await member.GetFromJsonAsync<CartResponse>("/api/v1/cart", Json);
        Assert.False(Assert.Single(cart!.Items).IsAvailable);
        Assert.True(cart.HasIssues);

        var removed = await member.DeleteAsync($"/api/v1/cart/items/{product.Variants[0].Id}");
        Assert.Empty((await removed.Content.ReadFromJsonAsync<CartResponse>(Json))!.Items);
    }

    [Fact]
    public async Task Addresses_keep_exactly_one_default()
    {
        var (member, _) = await TestAuth.VerifiedMemberAsync(factory);
        var first = await AddAddressAsync(member);
        var second = await (await member.PostAsJsonAsync("/api/v1/me/addresses",
            new SaveAddressRequest("Omar", "01111111111", "Cairo", "Zamalek", "26 July St", "3", null, IsDefault: false)))
            .Content.ReadFromJsonAsync<AddressResponse>();
        Assert.Equal(first, (await ListAddressesAsync(member)).Single(a => a.IsDefault).Id);

        var badPhone = await member.PostAsJsonAsync("/api/v1/me/addresses",
            new SaveAddressRequest("Omar", "call me", "Cairo", "Zamalek", "St", "3", null, false));
        Assert.Equal(HttpStatusCode.BadRequest, badPhone.StatusCode);

        await member.PutAsJsonAsync($"/api/v1/me/addresses/{second!.Id}", second with { IsDefault = true } is var a
            ? new SaveAddressRequest(a.FullName, a.Phone, a.City, a.Area, a.Street, a.Building, a.Notes, true) : null);
        Assert.Equal(second.Id, (await ListAddressesAsync(member)).Single(x => x.IsDefault).Id);

        await member.DeleteAsync($"/api/v1/me/addresses/{second.Id}");
        var left = Assert.Single(await ListAddressesAsync(member));
        Assert.True(left.IsDefault);
    }

    // ---------- helpers ----------

    private static Task<HttpResponseMessage> CheckoutAsync(HttpClient member, Guid addressId, PaymentMethod method) =>
        member.PostAsJsonAsync("/api/v1/orders", new CheckoutRequest(addressId, method), Json);

    private static async Task<OrderResponse> PlaceAsync(HttpClient member, Guid addressId, PaymentMethod method)
    {
        var response = await CheckoutAsync(member, addressId, method);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<OrderResponse>(Json))!;
    }

    private static async Task<OrderResponse?> ChangeStatusAsync(HttpClient admin, Guid orderId, OrderStatus status, HttpStatusCode expected)
    {
        var response = await admin.PutAsJsonAsync($"/api/v1/admin/orders/{orderId}/status", new ChangeOrderStatusRequest(status), Json);
        Assert.Equal(expected, response.StatusCode);
        return expected == HttpStatusCode.OK ? await response.Content.ReadFromJsonAsync<OrderResponse>(Json) : null;
    }

    private static async Task<List<AddressResponse>> ListAddressesAsync(HttpClient member) =>
        (await member.GetFromJsonAsync<List<AddressResponse>>("/api/v1/me/addresses", Json))!;

    private static async Task<string?> TitleAsync(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("title").GetString();
    }

    private async Task<int> CountLedgerAsync(Guid orderId, PointsEntryType type)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.PointsLedger.CountAsync(e => e.SourceType == PointsSourceType.Order && e.SourceId == orderId && e.Type == type);
    }
}
