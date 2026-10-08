namespace RowCycle.Infrastructure.Identity;

/// <summary>
/// A refresh token (FR-03). Only a SHA-256 hash is stored; the raw token is given to the client once.
/// Each token is used once: refreshing revokes it and issues a new one.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}
