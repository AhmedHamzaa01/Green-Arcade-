using System.Security.Claims;
using RowCycle.Infrastructure.Auth;

namespace RowCycle.Api.Controllers;

internal static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(AppClaimTypes.UserId)
            ?? throw new InvalidOperationException("Access token has no user id."));
}
