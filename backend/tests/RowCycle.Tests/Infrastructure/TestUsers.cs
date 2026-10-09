using Microsoft.Extensions.DependencyInjection;
using RowCycle.Domain.Entities;
using RowCycle.Infrastructure.Identity;
using RowCycle.Infrastructure.Persistence;

namespace RowCycle.Tests.Infrastructure;

public static class TestUsers
{
    /// <summary>Creates a user with a profile (balance 0) directly in the database.</summary>
    public static async Task<Guid> CreateAsync(ApiFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var email = $"user-{Guid.NewGuid():N}@example.com";
        var user = new AppUser { Id = Guid.CreateVersion7(), UserName = email, Email = email };
        db.Users.Add(user);
        db.UserProfiles.Add(new UserProfile { UserId = user.Id, FullName = "Test User" });
        await db.SaveChangesAsync();
        return user.Id;
    }
}
