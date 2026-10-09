using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RowCycle.Domain.Entities;

namespace RowCycle.Infrastructure.Persistence.Configurations;

internal sealed class PointsLedgerEntryConfiguration : IEntityTypeConfiguration<PointsLedgerEntry>
{
    public void Configure(EntityTypeBuilder<PointsLedgerEntry> builder)
    {
        builder.ToTable("points_ledger", t =>
            t.HasCheckConstraint("ck_points_ledger_amount_non_zero", "amount <> 0"));
        builder.HasUserForeignKey(x => x.UserId);
        builder.HasUserForeignKey(x => x.CreatedBy);

        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(x => new { x.UserId, x.CreatedAt }).IsDescending(false, true);
        // One Earn, one Redeem and one Reverse per submission/order; manual adjustments have no source id.
        builder.HasIndex(x => new { x.SourceType, x.SourceId, x.Type })
            .IsUnique()
            .HasFilter("source_id IS NOT NULL");
    }
}
