using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RowCycle.Domain.Entities;

namespace RowCycle.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders", t =>
        {
            t.HasCheckConstraint("ck_orders_total_egp_non_negative", "total_egp >= 0");
            t.HasCheckConstraint("ck_orders_total_points_non_negative", "total_points >= 0");
            t.HasCheckConstraint("ck_orders_total_reward_points_non_negative", "total_reward_points >= 0");
        });
        builder.HasUserForeignKey(x => x.UserId);

        builder.Property(x => x.OrderNumber).HasMaxLength(32);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        builder.OwnsOne(x => x.ShippingAddress, a => a.ToJson("shipping_address"));

        builder.HasIndex(x => x.OrderNumber).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.CreatedAt });
        builder.HasIndex(x => new { x.Status, x.CreatedAt });

        builder.HasMany(x => x.Items).WithOne().HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.StatusHistory).WithOne().HasForeignKey(h => h.OrderId).OnDelete(DeleteBehavior.Restrict);
    }
}
