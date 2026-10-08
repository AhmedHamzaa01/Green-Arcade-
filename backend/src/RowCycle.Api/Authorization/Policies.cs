using RowCycle.Domain.Constants;
using RowCycle.Infrastructure.Auth;

namespace RowCycle.Api.Authorization;

/// <summary>Authorization policies. Admin passes every role check (PRD roles).</summary>
public static class Policies
{
    /// <summary>Member with a confirmed email: required to submit actions and check out (FR-02).</summary>
    public const string VerifiedMember = nameof(VerifiedMember);

    public const string Moderator = nameof(Moderator);
    public const string StoreManager = nameof(StoreManager);
    public const string Admin = nameof(Admin);

    public static IServiceCollection AddAppAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(VerifiedMember, p => p
                .RequireRole(Roles.Member, Roles.Admin)
                .RequireClaim(AppClaimTypes.EmailVerified, "true"))
            .AddPolicy(Moderator, p => p.RequireRole(Roles.Moderator, Roles.Admin))
            .AddPolicy(StoreManager, p => p.RequireRole(Roles.StoreManager, Roles.Admin))
            .AddPolicy(Admin, p => p.RequireRole(Roles.Admin));
        return services;
    }
}
