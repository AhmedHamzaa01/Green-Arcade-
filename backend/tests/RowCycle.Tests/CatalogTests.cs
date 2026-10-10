using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Tests.Infrastructure;

namespace RowCycle.Tests;

[Collection(ApiCollection.Name)]
public sealed class CatalogTests(ApiFactory factory)
{
    /// <summary>A real 1×1 PNG.</summary>
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=");

    // Products from other tests share the database, so each test searches for its own random word.
    private readonly string _tag = $"t{Guid.NewGuid():N}"[..10];
    private readonly HttpClient _public = factory.CreateClient();

    [Fact]
    public async Task Public_catalog_hides_inactive_and_deleted_products_but_staff_see_inactive()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        await CreateProductAsync(admin, $"{_tag} active", 10m);
        var inactive = await CreateProductAsync(admin, $"{_tag} inactive", 10m, isActive: false);
        var deleted = await CreateProductAsync(admin, $"{_tag} deleted", 10m);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/admin/products/{deleted.Id}")).StatusCode);

        var shop = await SearchAsync($"?search={_tag}");
        Assert.Equal([$"{_tag} active"], shop.Items.Select(p => p.Name));

        Assert.Equal(HttpStatusCode.NotFound, (await _public.GetAsync($"/api/v1/products/{inactive.Slug}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _public.GetAsync($"/api/v1/products/{deleted.Slug}")).StatusCode);

        var staff = await admin.GetFromJsonAsync<PagedResult<AdminProductListItem>>($"/api/v1/admin/products?search={_tag}");
        Assert.Equal(2, staff!.Total);
        Assert.Contains(staff.Items, p => p.Name == $"{_tag} inactive" && !p.IsActive);
    }

    [Fact]
    public async Task Filters_by_category_searches_and_sorts()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        await CreateProductAsync(admin, $"{_tag} Cheap", 10m, category: "medals");
        await CreateProductAsync(admin, $"{_tag} Middle", 50m, category: "trophies");
        await CreateProductAsync(admin, $"{_tag} Pricey", 90m, category: "medals");

        Assert.Equal(3, (await SearchAsync($"?search={_tag.ToUpperInvariant()}")).Total);
        Assert.Equal(
            [$"{_tag} Cheap", $"{_tag} Pricey"],
            (await SearchAsync($"?search={_tag}&category=medals&sort=PriceAsc")).Items.Select(p => p.Name));
        Assert.Equal(
            [$"{_tag} Pricey", $"{_tag} Middle", $"{_tag} Cheap"],
            (await SearchAsync($"?search={_tag}&sort=PriceDesc")).Items.Select(p => p.Name));
        Assert.Equal(
            [$"{_tag} Pricey", $"{_tag} Middle", $"{_tag} Cheap"],
            (await SearchAsync($"?search={_tag}&sort=Newest")).Items.Select(p => p.Name));

        var page2 = await SearchAsync($"?search={_tag}&sort=PriceAsc&page=2&pageSize=2");
        Assert.Equal(3, page2.Total);
        Assert.Equal([$"{_tag} Pricey"], page2.Items.Select(p => p.Name));

        // % and _ are matched literally, not as wildcards.
        Assert.Equal(0, (await SearchAsync($"?search={_tag}%25")).Total);
    }

    [Fact]
    public async Task Product_detail_shows_variants_images_and_reward_points()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var product = await CreateProductAsync(admin, $"{_tag} Medal", 120m, rewardPoints: 30, pricePoints: 400);
        Assert.False((await GetPublicAsync(product.Slug)).InStock);

        await SetStockAsync(admin, product, 5);
        var detail = await GetPublicAsync(product.Slug);

        Assert.True(detail.InStock);
        Assert.Equal(30, detail.RewardPoints);
        Assert.Equal(400, detail.PricePoints);
        var variant = Assert.Single(detail.Variants);
        Assert.Equal("Default", variant.Name);
        Assert.Equal(120m, variant.PriceEgp);
        Assert.True(variant.InStock);
    }

    [Fact]
    public async Task Inactive_category_hides_its_products()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var category = await CreateCategoryAsync(admin, $"{_tag} Category");
        var product = await CreateProductAsync(admin, $"{_tag} In category", 10m, categoryId: category.Id);
        Assert.Equal(1, (await SearchAsync($"?search={_tag}")).Total);

        await admin.PutAsJsonAsync($"/api/v1/admin/product-categories/{category.Id}",
            new SaveProductCategoryRequest(category.Name, category.Slug, category.SortOrder, IsActive: false));

        Assert.Equal(0, (await SearchAsync($"?search={_tag}")).Total);
        Assert.Equal(HttpStatusCode.NotFound, (await _public.GetAsync($"/api/v1/products/{product.Slug}")).StatusCode);
        var publicCategories = await _public.GetFromJsonAsync<List<ProductCategoryResponse>>("/api/v1/product-categories");
        Assert.DoesNotContain(publicCategories!, c => c.Id == category.Id);
    }

    [Fact]
    public async Task Slugs_are_made_from_names_and_stay_unique()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var first = await CreateProductAsync(admin, $"{_tag} Gold Medal!", 10m);
        var second = await CreateProductAsync(admin, $"{_tag} Gold Medal!", 10m);

        Assert.Equal($"{_tag}-gold-medal", first.Slug);
        Assert.Equal($"{_tag}-gold-medal-2", second.Slug);

        var taken = await admin.PostAsJsonAsync("/api/v1/admin/products", NewProduct(await CategoryIdAsync(admin, "medals"), "Other", slug: first.Slug));
        Assert.Equal(HttpStatusCode.Conflict, taken.StatusCode);

        // A deleted product's slug can be used again.
        await admin.DeleteAsync($"/api/v1/admin/products/{first.Id}");
        var reused = await admin.PostAsJsonAsync("/api/v1/admin/products", NewProduct(await CategoryIdAsync(admin, "medals"), "Other", slug: first.Slug));
        Assert.Equal(HttpStatusCode.Created, reused.StatusCode);
    }

    [Fact]
    public async Task Variant_rules_are_enforced()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var product = await CreateProductAsync(admin, $"{_tag} Shirt", 200m);
        var defaultVariant = Assert.Single(product.Variants);

        var negative = await admin.PutAsJsonAsync($"/api/v1/admin/products/{product.Id}/variants/{defaultVariant.Id}",
            new SaveProductVariantRequest("Default", defaultVariant.Sku, -1, null));
        Assert.Equal(HttpStatusCode.BadRequest, negative.StatusCode);

        var duplicateSku = await admin.PostAsJsonAsync($"/api/v1/admin/products/{product.Id}/variants",
            new SaveProductVariantRequest("Large", defaultVariant.Sku, 3, null));
        Assert.Equal(HttpStatusCode.Conflict, duplicateSku.StatusCode);

        var lastOne = await admin.DeleteAsync($"/api/v1/admin/products/{product.Id}/variants/{defaultVariant.Id}");
        Assert.Equal(HttpStatusCode.Conflict, lastOne.StatusCode);

        var added = await (await admin.PostAsJsonAsync($"/api/v1/admin/products/{product.Id}/variants",
            new SaveProductVariantRequest("Large", $"{_tag}-L", 3, 250m))).Content.ReadFromJsonAsync<AdminProductResponse>();
        Assert.Equal(2, added!.Variants.Count);

        var afterDelete = await (await admin.DeleteAsync($"/api/v1/admin/products/{product.Id}/variants/{defaultVariant.Id}"))
            .Content.ReadFromJsonAsync<AdminProductResponse>();
        var remaining = Assert.Single(afterDelete!.Variants);
        Assert.Equal("Large", remaining.Name);

        var detail = await GetPublicAsync(product.Slug);
        Assert.Equal(250m, Assert.Single(detail.Variants).PriceEgp);
    }

    [Fact]
    public async Task Image_upload_checks_the_file_and_serves_it()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var product = await CreateProductAsync(admin, $"{_tag} Trophy", 10m);

        var first = await UploadAsync(admin, product.Id, Png, "photo.png");
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var withOne = (await first.Content.ReadFromJsonAsync<AdminProductResponse>())!;
        var image = Assert.Single(withOne.Images);
        Assert.Matches("^/media/products/[0-9a-f]{32}\\.png$", image.Url);

        var served = await _public.GetAsync(image.Url);
        Assert.Equal(HttpStatusCode.OK, served.StatusCode);
        Assert.Equal("image/png", served.Content.Headers.ContentType?.MediaType);
        Assert.Equal("nosniff", Assert.Single(served.Headers.GetValues("X-Content-Type-Options")));

        // Renamed text file: refused because its bytes aren't an image.
        var fake = await UploadAsync(admin, product.Id, "not an image"u8.ToArray(), "evil.png");
        Assert.Equal(HttpStatusCode.BadRequest, fake.StatusCode);

        var tooBig = new byte[ImageFileCheck.MaxBytes + 1];
        Png.CopyTo(tooBig, 0);
        Assert.Equal(HttpStatusCode.BadRequest, (await UploadAsync(admin, product.Id, tooBig, "big.png")).StatusCode);

        // Second image, then put it first.
        var withTwo = (await (await UploadAsync(admin, product.Id, Png, "second.png")).Content.ReadFromJsonAsync<AdminProductResponse>())!;
        var ids = withTwo.Images.Select(i => i.Id).Reverse().ToList();
        var reordered = (await (await admin.PutAsJsonAsync($"/api/v1/admin/products/{product.Id}/images/order", new ReorderImagesRequest(ids)))
            .Content.ReadFromJsonAsync<AdminProductResponse>())!;
        Assert.Equal(ids, reordered.Images.Select(i => i.Id));

        var afterDelete = (await (await admin.DeleteAsync($"/api/v1/admin/products/{product.Id}/images/{image.Id}"))
            .Content.ReadFromJsonAsync<AdminProductResponse>())!;
        Assert.DoesNotContain(afterDelete.Images, i => i.Id == image.Id);
        Assert.DoesNotContain((await GetPublicAsync(product.Slug)).Images, i => i.Id == image.Id);
    }

    [Fact]
    public async Task Category_with_products_cannot_be_deleted()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var full = await CreateCategoryAsync(admin, $"{_tag} Full");
        await CreateProductAsync(admin, $"{_tag} Inside", 10m, categoryId: full.Id);
        var empty = await CreateCategoryAsync(admin, $"{_tag} Empty");

        Assert.Equal(HttpStatusCode.Conflict, (await admin.DeleteAsync($"/api/v1/admin/product-categories/{full.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/admin/product-categories/{empty.Id}")).StatusCode);

        var all = await admin.GetFromJsonAsync<List<ProductCategoryResponse>>("/api/v1/admin/product-categories");
        Assert.Contains(all!, c => c.Id == full.Id);
        Assert.DoesNotContain(all!, c => c.Id == empty.Id);
    }

    [Fact]
    public async Task Members_cannot_manage_the_catalog_but_anyone_can_browse()
    {
        var member = await TestAuth.MemberClientAsync(factory);

        Assert.Equal(HttpStatusCode.Forbidden, (await member.GetAsync("/api/v1/admin/products")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await member.PostAsJsonAsync("/api/v1/admin/product-categories", new SaveProductCategoryRequest("X", null, 0, true))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _public.GetAsync("/api/v1/admin/products")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _public.GetAsync("/api/v1/products")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _public.GetAsync("/api/v1/product-categories")).StatusCode);
    }

    [Fact]
    public async Task Catalog_changes_are_audit_logged()
    {
        var admin = await TestAuth.AdminClientAsync(factory);
        var product = await CreateProductAsync(admin, $"{_tag} Logged", 10m);
        await SetStockAsync(admin, product, 7);

        var logs = await admin.GetFromJsonAsync<PagedResult<AuditLogResponse>>("/api/v1/admin/audit-logs?pageSize=100");
        Assert.Contains(logs!.Items, l => l.Action == "product.create" && l.EntityId == product.Id.ToString());
        var stockChange = Assert.Single(logs.Items, l => l.Action == "product_variant.update" && l.EntityId == product.Variants[0].Id.ToString());
        Assert.Equal(0, stockChange.Data!.Value.GetProperty("before").GetProperty("stock").GetInt32());
        Assert.Equal(7, stockChange.Data!.Value.GetProperty("after").GetProperty("stock").GetInt32());
    }

    // ---------- helpers ----------

    private async Task<PagedResult<ProductListItem>> SearchAsync(string query) =>
        (await _public.GetFromJsonAsync<PagedResult<ProductListItem>>($"/api/v1/products{query}"))!;

    private async Task<ProductDetailResponse> GetPublicAsync(string slug) =>
        (await _public.GetFromJsonAsync<ProductDetailResponse>($"/api/v1/products/{slug}"))!;

    private static async Task<Guid> CategoryIdAsync(HttpClient admin, string slug) =>
        (await admin.GetFromJsonAsync<List<ProductCategoryResponse>>("/api/v1/admin/product-categories"))!.Single(c => c.Slug == slug).Id;

    private static SaveProductRequest NewProduct(
        Guid categoryId, string name, decimal price = 10m, bool isActive = true, int rewardPoints = 0, int? pricePoints = null, string? slug = null) =>
        new(categoryId, name, slug, "Made from recycled cans.", price, pricePoints, rewardPoints, isActive);

    private static async Task<AdminProductResponse> CreateProductAsync(
        HttpClient admin, string name, decimal price, bool isActive = true, string category = "medals",
        Guid? categoryId = null, int rewardPoints = 0, int? pricePoints = null)
    {
        var request = NewProduct(categoryId ?? await CategoryIdAsync(admin, category), name, price, isActive, rewardPoints, pricePoints);
        var response = await admin.PostAsJsonAsync("/api/v1/admin/products", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AdminProductResponse>())!;
    }

    private static async Task<ProductCategoryResponse> CreateCategoryAsync(HttpClient admin, string name)
    {
        var response = await admin.PostAsJsonAsync("/api/v1/admin/product-categories", new SaveProductCategoryRequest(name, null, 10, true));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProductCategoryResponse>())!;
    }

    private static async Task SetStockAsync(HttpClient admin, AdminProductResponse product, int stock)
    {
        var variant = product.Variants[0];
        var response = await admin.PutAsJsonAsync($"/api/v1/admin/products/{product.Id}/variants/{variant.Id}",
            new SaveProductVariantRequest(variant.Name, variant.Sku, stock, variant.PriceOverride));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static Task<HttpResponseMessage> UploadAsync(HttpClient admin, Guid productId, byte[] bytes, string fileName)
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        var form = new MultipartFormDataContent { { file, "file", fileName } };
        return admin.PostAsync($"/api/v1/admin/products/{productId}/images", form);
    }
}
