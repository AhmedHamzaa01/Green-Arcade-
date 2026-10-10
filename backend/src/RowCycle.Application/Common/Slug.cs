using System.Text;
using System.Text.RegularExpressions;

namespace RowCycle.Application.Common;

/// <summary>URL-friendly names: "Gold Medal (2026)" → "gold-medal-2026".</summary>
public static partial class Slug
{
    public const string Pattern = "^[a-z0-9]+(-[a-z0-9]+)*$";

    public static string From(string text, string fallback = "item")
    {
        var builder = new StringBuilder();
        foreach (var c in text.Trim().ToLowerInvariant())
        {
            builder.Append(c is >= 'a' and <= 'z' or >= '0' and <= '9' ? c : '-');
        }

        var slug = MultipleDashes().Replace(builder.ToString(), "-").Trim('-');
        if (slug.Length > 80)
        {
            slug = slug[..80].TrimEnd('-');
        }

        return slug.Length == 0 ? fallback : slug;
    }

    /// <summary>The first of "slug", "slug-2", "slug-3"… for which <paramref name="isTaken"/> is false.</summary>
    public static async Task<string> MakeUniqueAsync(string slug, Func<string, Task<bool>> isTaken)
    {
        var candidate = slug;
        for (var n = 2; await isTaken(candidate); n++)
        {
            candidate = $"{slug}-{n}";
        }

        return candidate;
    }

    [GeneratedRegex("-{2,}")]
    private static partial Regex MultipleDashes();
}
