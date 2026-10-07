using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RowCycle.Domain.Entities;

namespace RowCycle.Infrastructure.Persistence.Configurations;

internal sealed class UserBadgeConfiguration : IEntityTypeConfiguration<UserBadge>
{
    public void Configure(EntityTypeBuilder<UserBadge> builder)
    {
        builder.ToTable("user_badges");
        builder.HasKey(x => new { x.UserId, x.BadgeId });
        builder.HasUserForeignKey(x => x.UserId, DeleteBehavior.Cascade);
        builder.HasOne(x => x.Badge).WithMany().HasForeignKey(x => x.BadgeId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(x => x.AwardedAt).HasDefaultValueSql("now()");
    }
}
