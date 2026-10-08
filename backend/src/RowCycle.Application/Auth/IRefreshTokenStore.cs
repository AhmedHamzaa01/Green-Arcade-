namespace RowCycle.Application.Auth;

/// <summary>Refresh token storage (FR-03). Only hashes are stored; raw tokens leave the system once.</summary>
public interface IRefreshTokenStore
{
    /// <summary>Creates a token for <paramref name="userId"/>, valid for the configured number of days.</summary>
    Task<IssuedRefreshToken> CreateAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<StoredRefreshToken?> FindAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>Revokes the token if it's still active. False means it was already revoked (e.g. a parallel refresh won).</summary>
    Task<bool> TryRevokeAsync(Guid tokenId, CancellationToken cancellationToken = default);

    Task RevokeAsync(string token, CancellationToken cancellationToken = default);

    Task RevokeAllAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed record IssuedRefreshToken(string Token, DateTimeOffset ExpiresAt);

public sealed record StoredRefreshToken(Guid Id, Guid UserId, DateTimeOffset ExpiresAt, DateTimeOffset? RevokedAt);
