namespace RowCycle.Domain.Constants;

/// <summary>
/// The icons an admin can pick for a badge (Material Symbols names, shown by the web app).
/// A fixed set instead of uploads (DECISIONS 2026-10-13).
/// </summary>
public static class BadgeIcons
{
    public const string Default = "emoji_events";

    public static readonly IReadOnlyList<string> All =
    [
        "emoji_events",
        "military_tech",
        "workspace_premium",
        "star",
        "recycling",
        "eco",
        "park",
        "water_drop",
        "pets",
        "volunteer_activism",
        "favorite",
        "local_shipping",
    ];
}
