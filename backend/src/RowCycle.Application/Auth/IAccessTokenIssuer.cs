namespace RowCycle.Application.Auth;

/// <summary>Creates the signed access token (JWT, FR-03).</summary>
public interface IAccessTokenIssuer
{
    IssuedAccessToken Issue(UserAccount user, IReadOnlyList<string> roles);
}

public sealed record IssuedAccessToken(string Token, DateTimeOffset ExpiresAt);
