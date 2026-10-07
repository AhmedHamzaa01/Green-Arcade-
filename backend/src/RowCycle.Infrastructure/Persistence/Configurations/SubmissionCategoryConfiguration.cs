using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RowCycle.Domain.Entities;

namespace RowCycle.Infrastructure.Persistence.Configurations;

internal sealed class SubmissionCategoryConfiguration : IEntityTypeConfiguration<SubmissionCategory>
{
    public void Configure(EntityTypeBuilder<SubmissionCategory> builder)
    {
        builder.ToTable("submission_categories", t =>
            t.HasCheckConstraint("ck_submission_categories_points_reward_non_negative", "points_reward >= 0"));
        builder.Property(x => x.Name).HasMaxLength(100);
        builder.HasIndex(x => x.Name).IsUnique().HasFilter("deleted_at IS NULL");
    }
}
