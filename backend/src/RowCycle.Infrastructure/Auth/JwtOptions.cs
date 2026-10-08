using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace RowCycle.Infrastructure.Auth;

/// <summary>Section <c>Jwt</c>. The signing key comes from user-secrets or env vars, never appsettings (NFR-03).</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>HMAC-SHA256 key, at least 32 characters.</summary>
    [Required, MinLength(32)]
    public string SigningKey { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int AccessTokenMinutes { get; set; } = 15;

    [Range(1, 90)]
    public int RefreshTokenDays { get; set; } = 7;

    public SymmetricSecurityKey CreateSigningKey() => new(Encoding.UTF8.GetBytes(SigningKey));
}
