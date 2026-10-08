using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using RowCycle.Application.Auth;

namespace RowCycle.Infrastructure.Auth;

/// <summary>Signs access tokens with HMAC-SHA256 using the <c>Jwt</c> settings.</summary>
internal sealed class JwtAccessTokenIssuer(IOptions<JwtOptions> options, TimeProvider timeProvider) : IAccessTokenIssuer
{
    private readonly JsonWebTokenHandler _handler = new();

    public IssuedAccessToken Issue(UserAccount user, IReadOnlyList<string> roles)
    {
        var jwt = options.Value;
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(jwt.AccessTokenMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            Claims = new Dictionary<string, object>
            {
                [AppClaimTypes.UserId] = user.Id.ToString(),
                [AppClaimTypes.Email] = user.Email,
                [AppClaimTypes.EmailVerified] = user.EmailConfirmed,
                [AppClaimTypes.Role] = roles.ToArray(),
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
            },
            SigningCredentials = new SigningCredentials(jwt.CreateSigningKey(), SecurityAlgorithms.HmacSha256),
        };

        return new IssuedAccessToken(_handler.CreateToken(descriptor), expiresAt);
    }
}
