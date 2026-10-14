using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RowCycle.Domain.Entities;

namespace RowCycle.Infrastructure.Persistence.Configurations;

internal sealed class BadgeConfiguration : IEntityTypeConfiguration<Badge>
{
    public void Configure(EntityTypeBuilder<Badge> builder)
    {
        builder.ToTable("badges", t =>
            t.HasCheckConstraint("ck_badges_threshold_non_negative", "threshold >= 0"));
        builder.Property(x => x.Name).HasMaxLength(100);
        builder.HasIndex(x => x.Name).IsUnique().HasFilter("deleted_at IS NULL");
        builder.Property(x => x.Icon).HasMaxLength(50);
        builder.HasOne<SubmissionCategory>().WithMany().HasForeignKey(x => x.RuleCategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}
