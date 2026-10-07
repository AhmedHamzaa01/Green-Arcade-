using Microsoft.EntityFrameworkCore;
using RowCycle.Domain.Constants;
using RowCycle.Domain.Entities;
using RowCycle.Infrastructure.Identity;

namespace RowCycle.Infrastructure.Persistence;

/// <summary>Reference data written by the migrations. Ids and stamps are fixed so migrations stay stable.</summary>
internal static class SeedData
{
    private const int DefaultSubmissionPoints = 10; // DECISIONS 2026-10-04
    private const int DefaultDailySubmissionLimit = 5; // FR-09

    public static void Apply(ModelBuilder builder)
    {
        builder.Entity<AppRole>().HasData(
            Role("0538d610-3693-4e18-897a-f55a6ed072c4", Roles.Member),
            Role("536119e5-23b3-4db8-9c7c-1d82769398bf", Roles.Moderator),
            Role("3e93106b-961d-41f7-abe3-e45a3a172d92", Roles.StoreManager),
            Role("c5515a97-9769-434d-a87e-fc9bc7fde787", Roles.Admin));

        // PRD F3. Can collection and clean-up record weight_kg.
        builder.Entity<SubmissionCategory>().HasData(
            SubmissionCategory("17ce849a-9c7c-46d9-b5e4-aad889192218", "Can collection", tracksWeight: true),
            SubmissionCategory("3a236a25-ac7c-47a1-b914-97eec2772c8e", "Coastal/lake clean-up", tracksWeight: true),
            SubmissionCategory("865079da-4bd6-4715-950b-dca027c9f711", "Recycling", tracksWeight: false),
            SubmissionCategory("c0b678f5-740b-4a83-8c94-d0b9330871b7", "Feeding animals", tracksWeight: false),
            SubmissionCategory("dbb6cd09-5c4b-4830-8680-344995ce1079", "Helping people", tracksWeight: false),
            SubmissionCategory("a7879a02-a50c-4bf4-bccc-ddd893c46334", "Other", tracksWeight: false));

        // PRD F5.
        builder.Entity<ProductCategory>().HasData(
            ProductCategory("52629265-d063-46a3-9594-2fbac9ac63b0", "Medals", "medals", 1),
            ProductCategory("1d68bd30-bd19-4df5-b750-fe1014a4e3c7", "Trophies", "trophies", 2),
            ProductCategory("01f0b21c-d090-42d2-a67b-7ed18b67599d", "Recycled-aluminium goods", "recycled-aluminium-goods", 3),
            ProductCategory("b3d85dc1-e1d2-4a6f-bf53-6fd374a3bce0", "Branded merch", "branded-merch", 4));

        builder.Entity<Setting>().HasData(
            new Setting { Key = SettingKeys.DailySubmissionLimit, Value = DefaultDailySubmissionLimit.ToString() });
    }

    private static AppRole Role(string id, string name) => new()
    {
        Id = Guid.Parse(id),
        Name = name,
        NormalizedName = name.ToUpperInvariant(),
        ConcurrencyStamp = id,
    };

    private static SubmissionCategory SubmissionCategory(string id, string name, bool tracksWeight) => new()
    {
        Id = Guid.Parse(id),
        Name = name,
        PointsReward = DefaultSubmissionPoints,
        TracksWeight = tracksWeight,
        IsActive = true,
    };

    private static ProductCategory ProductCategory(string id, string name, string slug, int sortOrder) => new()
    {
        Id = Guid.Parse(id),
        Name = name,
        Slug = slug,
        SortOrder = sortOrder,
        IsActive = true,
    };
}
