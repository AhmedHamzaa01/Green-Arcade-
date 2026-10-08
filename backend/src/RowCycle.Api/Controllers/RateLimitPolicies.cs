using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace RowCycle.Api.Controllers;

public static class RateLimitPolicies
{
    public const string Auth = "auth";

    /// <summary>
    /// /auth/* allows <c>RateLimiting:Auth:PermitLimit</c> requests per <c>RateLimiting:Auth:WindowSeconds</c> per IP (NFR-03).
    /// </summary>
    public static IServiceCollection AddAppRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var permitLimit = configuration.GetValue("RateLimiting:Auth:PermitLimit", 10);
        var window = TimeSpan.FromSeconds(configuration.GetValue("RateLimiting:Auth:WindowSeconds", 60));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(Auth, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = permitLimit, Window = window, QueueLimit = 0 }));
        });
        return services;
    }
}
