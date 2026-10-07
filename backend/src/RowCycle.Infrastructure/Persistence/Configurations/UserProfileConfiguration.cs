using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RowCycle.Domain.Entities;

namespace RowCycle.Infrastructure.Persistence.Configurations;

internal sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles", t =>
            t.HasCheckConstraint("ck_user_profiles_points_balance_non_negative", "points_balance >= 0"));
        builder.HasKey(x => x.UserId);
        builder.Property(x => x.UserId).ValueGeneratedNever();
        builder.HasUserForeignKey(x => x.UserId, DeleteBehavior.Cascade);

        builder.Property(x => x.FullName).HasMaxLength(200);
        builder.Property(x => x.PhotoUrl).HasMaxLength(500);
        builder.Property(x => x.Phone).HasMaxLength(32);

        builder.HasMany(x => x.Addresses).WithOne().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
