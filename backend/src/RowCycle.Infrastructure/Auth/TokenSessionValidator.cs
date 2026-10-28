using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using RowCycle.Infrastructure.Persistence;

namespace RowCycle.Infrastructure.Auth;

/// <summary>
/// Runs after an access token's signature and expiry were checked: the user must still be active and the token's stamp
/// must match the user's current security stamp. Blocking a user or changing their roles changes the stamp, so their
/// existing tokens stop working at once (NFR-03). One primary-key lookup per authenticated request.
/// </summary>
public sealed class TokenSessionValidator(AppDbContext db)
{
    /// <summary>Null if the token is still valid, otherwise why not (for the log).</summary>
    public async Task<string?> ValidateAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(principal.FindFirstValue(AppClaimTypes.UserId), out var userId))
        {
            return "no user id";
        }

        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.IsActive, u.SecurityStamp })
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null || !user.IsActive)
        {
            return "user missing or blocked";
        }

        return principal.FindFirstValue(AppClaimTypes.Stamp) == (user.SecurityStamp ?? string.Empty)
            ? null
            : "security stamp changed";
    }
}
