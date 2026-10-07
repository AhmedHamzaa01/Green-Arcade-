using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RowCycle.Domain.Entities;

namespace RowCycle.Infrastructure.Persistence.Configurations;

internal sealed class SubmissionConfiguration : IEntityTypeConfiguration<Submission>
{
    public void Configure(EntityTypeBuilder<Submission> builder)
    {
        builder.ToTable("submissions", t =>
            t.HasCheckConstraint("ck_submissions_weight_kg_non_negative", "weight_kg IS NULL OR weight_kg >= 0"));
        builder.HasUserForeignKey(x => x.UserId);
        builder.HasUserForeignKey(x => x.ReviewedBy);
        builder.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.PhotoUrl).HasMaxLength(500);
        builder.Property(x => x.Note).HasMaxLength(1000);
        builder.Property(x => x.WeightKg).HasPrecision(8, 2);
        builder.Property(x => x.RejectReason).HasMaxLength(500);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(x => new { x.UserId, x.CreatedAt });
        builder.HasIndex(x => new { x.Status, x.CreatedAt });
    }
}
