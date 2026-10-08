using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RowCycle.Application.Auth;
using RowCycle.Infrastructure.Identity;
using RowCycle.Infrastructure.Persistence;

namespace RowCycle.Infrastructure.Auth;

/// <summary>Stores SHA-256 hashes of refresh tokens in <c>refresh_tokens</c>.</summary>
internal sealed class RefreshTokenStore(AppDbContext db, IOptions<JwtOptions> options, TimeProvider timeProvider) : IRefreshTokenStore
{
    public async Task<IssuedRefreshToken> CreateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddDays(options.Value.RefreshTokenDays);

        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            TokenHash = Hash(token),
            CreatedAt = now,
            ExpiresAt = expiresAt,
        });
        await db.SaveChangesAsync(cancellationToken);

        return new IssuedRefreshToken(token, expiresAt);
    }

    public async Task<StoredRefreshToken?> FindAsync(string token, CancellationToken cancellationToken = default)
    {
        var hash = Hash(token);
        return await db.RefreshTokens.AsNoTracking()
            .Where(t => t.TokenHash == hash)
            .Select(t => new StoredRefreshToken(t.Id, t.UserId, t.ExpiresAt, t.RevokedAt))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> TryRevokeAsync(Guid tokenId, CancellationToken cancellationToken = default)
    {
        // A conditional UPDATE: only one of two parallel requests can revoke the same token.
        var now = timeProvider.GetUtcNow();
        var updated = await db.RefreshTokens
            .Where(t => t.Id == tokenId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), cancellationToken);
        return updated == 1;
    }

    public async Task RevokeAsync(string token, CancellationToken cancellationToken = default)
    {
        var hash = Hash(token);
        var now = timeProvider.GetUtcNow();
        await db.RefreshTokens
            .Where(t => t.TokenHash == hash && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), cancellationToken);
    }

    public async Task RevokeAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), cancellationToken);
    }

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
