using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RowCycle.Domain.Entities;

namespace RowCycle.Infrastructure.Persistence.Configurations;

internal sealed class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
    {
        builder.ToTable("order_status_history");
        builder.HasUserForeignKey(x => x.ChangedBy);
        builder.Property(x => x.ChangedAt).HasDefaultValueSql("now()");
        builder.HasIndex(x => new { x.OrderId, x.ChangedAt });
    }
}
