using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RowCycle.Domain.Entities;
using RowCycle.Domain.Enums;
using RowCycle.Infrastructure.Identity;
using RowCycle.Infrastructure.Persistence;
using RowCycle.Tests.Infrastructure;

namespace RowCycle.Tests;

[Collection(ApiCollection.Name)]
public sealed class SoftDeleteTests(ApiFactory factory)
{
    [Fact]
    public async Task Deleting_a_product_keeps_the_row_but_hides_it()
    {
        var slug = Unique("medal");
        Guid productId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var product = await AddProductAsync(db, slug);
            productId = product.Id;

            db.Products.Remove(product);
            await db.SaveChangesAsync();
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            Assert.False(await db.Products.AnyAsync(p => p.Id == productId));

            var stored = await db.Products.IgnoreQueryFilters().SingleAsync(p => p.Id == productId);
            Assert.NotNull(stored.DeletedAt);
        }
    }

    [Fact]
    public async Task Slug_of_a_deleted_product_can_be_reused()
    {
        var slug = Unique("medal");
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var first = await AddProductAsync(db, slug);
        db.Products.Remove(first);
        await db.SaveChangesAsync();

        var second = await AddProductAsync(db, slug);

        Assert.Equal(second.Id, (await db.Products.SingleAsync(p => p.Slug == slug)).Id);
    }

    [Fact]
    public async Task Ledger_rows_cannot_be_deleted()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var email = $"{Unique("user")}@example.com";
        var user = new AppUser { Id = Guid.NewGuid(), UserName = email, Email = email };
        db.Users.Add(user);
        db.UserProfiles.Add(new UserProfile { UserId = user.Id, FullName = "Test", PointsBalance = 10 });
        var entry = new PointsLedgerEntry
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Amount = 10,
            Type = PointsEntryType.Adjust,
            SourceType = PointsSourceType.Manual,
            Reason = "test",
        };
        db.PointsLedger.Add(entry);
        await db.SaveChangesAsync();

        db.PointsLedger.Remove(entry);

        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    private static async Task<Product> AddProductAsync(AppDbContext db, string slug)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = await db.ProductCategories.Select(c => c.Id).FirstAsync(),
            Name = "Test medal",
            Slug = slug,
            PriceEgp = 100m,
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product;
    }

    private static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}";
}
