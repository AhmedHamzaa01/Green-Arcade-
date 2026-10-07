using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using RowCycle.Domain.Constants;
using RowCycle.Domain.Entities;
using RowCycle.Infrastructure.Identity;
using RowCycle.Infrastructure.Persistence;
using RowCycle.Tests.Infrastructure;

namespace RowCycle.Tests;

[Collection(ApiCollection.Name)]
public sealed class DatabaseSchemaTests(ApiFactory factory)
{
    [Fact]
    public async Task Migrations_are_applied_and_match_the_model()
    {
        await using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.False(db.Database.HasPendingModelChanges(), "Model has changes that are not in a migration.");
    }

    [Fact]
    public async Task Reference_data_is_seeded()
    {
        await using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var roles = await db.Roles.Select(r => r.Name).ToListAsync();
        Assert.Equivalent(new[] { Roles.Member, Roles.Moderator, Roles.StoreManager, Roles.Admin }, roles);

        var submissionCategories = await db.SubmissionCategories.ToListAsync();
        Assert.Equal(6, submissionCategories.Count);
        Assert.All(submissionCategories, c => Assert.Equal(10, c.PointsReward));
        Assert.Equivalent(
            new[] { "Can collection", "Coastal/lake clean-up" },
            submissionCategories.Where(c => c.TracksWeight).Select(c => c.Name));

        var productCategories = await db.ProductCategories.OrderBy(c => c.SortOrder).Select(c => c.Slug).ToListAsync();
        Assert.Equal(["medals", "trophies", "recycled-aluminium-goods", "branded-merch"], productCategories);

        var limit = await db.Settings.SingleAsync(s => s.Key == SettingKeys.DailySubmissionLimit);
        Assert.Equal("5", limit.Value);
    }

    [Fact]
    public async Task Points_balance_cannot_go_below_zero()
    {
        await AssertCheckViolationAsync("ck_user_profiles_points_balance_non_negative", async db =>
        {
            var user = AddUser(db);
            db.UserProfiles.Add(new UserProfile { UserId = user.Id, FullName = "Test", PointsBalance = -1 });
            await db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task Variant_stock_cannot_go_below_zero()
    {
        await AssertCheckViolationAsync("ck_product_variants_stock_non_negative", async db =>
        {
            var product = AddProduct(db, rewardPoints: 0);
            product.Variants.Add(new ProductVariant { Id = Guid.NewGuid(), Name = "Default", Sku = Unique("sku"), Stock = -1 });
            await db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task Product_reward_points_cannot_be_negative()
    {
        await AssertCheckViolationAsync("ck_products_reward_points_non_negative", async db =>
        {
            AddProduct(db, rewardPoints: -1);
            await db.SaveChangesAsync();
        });
    }

    private AsyncServiceScope CreateScope() => factory.Services.CreateAsyncScope();

    private async Task AssertCheckViolationAsync(string constraint, Func<AppDbContext, Task> act)
    {
        await using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => act(db));

        var pg = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal(PostgresErrorCodes.CheckViolation, pg.SqlState);
        Assert.Equal(constraint, pg.ConstraintName);
    }

    private static AppUser AddUser(AppDbContext db)
    {
        var email = $"{Unique("user")}@example.com";
        var user = new AppUser { Id = Guid.NewGuid(), UserName = email, Email = email };
        db.Users.Add(user);
        return user;
    }

    private static Product AddProduct(AppDbContext db, int rewardPoints)
    {
        var categoryId = db.ProductCategories.Select(c => c.Id).First();
        var product = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = categoryId,
            Name = "Test medal",
            Slug = Unique("medal"),
            PriceEgp = 100m,
            RewardPoints = rewardPoints,
        };
        db.Products.Add(product);
        return product;
    }

    private static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}";
}
