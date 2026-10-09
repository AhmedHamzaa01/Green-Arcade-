using RowCycle.Application.Dtos;

namespace RowCycle.Api.Controllers;

/// <summary>
/// The refresh token lives only in this cookie (FR-03, NFR-03):
/// httpOnly (page scripts can't read it), SameSite=Strict (not sent from other sites),
/// limited to the auth endpoints, and Secure everywhere except local Development.
/// </summary>
internal static class RefreshTokenCookie
{
    public const string Name = "rc_refresh";
    private const string Path = "/api/v1/auth";

    public static string? Read(HttpRequest request) => request.Cookies[Name];

    public static void Write(HttpResponse response, AuthSession session, bool secure) =>
        response.Cookies.Append(Name, session.RefreshToken, Options(secure, session.RefreshTokenExpiresAt));

    public static void Delete(HttpResponse response, bool secure) =>
        response.Cookies.Delete(Name, Options(secure, expires: null));

    private static CookieOptions Options(bool secure, DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = secure,
        SameSite = SameSiteMode.Strict,
        Path = Path,
        Expires = expires,
        IsEssential = true,
    };
}
