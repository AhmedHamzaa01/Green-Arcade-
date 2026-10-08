using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RowCycle.Domain.Constants;
using RowCycle.Domain.Entities;
using RowCycle.Infrastructure.Auth;
using RowCycle.Infrastructure.Persistence;

namespace RowCycle.Infrastructure.Identity;

/// <summary>Creates the first admin from the <c>Admin</c> config section (user-secrets / env vars) if it doesn't exist.</summary>
internal static class AdminSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<AdminSeedOptions>>().Value;
        if (string.IsNullOrWhiteSpace(options.Email) || string.IsNullOrWhiteSpace(options.Password))
        {
            return;
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(AdminSeeder));

        var user = await userManager.FindByEmailAsync(options.Email);
        if (user is null)
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            user = new AppUser { Id = Guid.CreateVersion7(), UserName = options.Email, Email = options.Email, EmailConfirmed = true };

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            Check(await userManager.CreateAsync(user, options.Password));
            db.UserProfiles.Add(new UserProfile { UserId = user.Id, FullName = options.FullName });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            logger.LogInformation("Created admin user {Email}", options.Email);
        }

        if (!await userManager.IsInRoleAsync(user, Roles.Admin))
        {
            Check(await userManager.AddToRoleAsync(user, Roles.Admin));
        }
    }

    private static void Check(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                "Admin seed failed: " + string.Join(" ", result.Errors.Select(e => e.Description)));
        }
    }
}
