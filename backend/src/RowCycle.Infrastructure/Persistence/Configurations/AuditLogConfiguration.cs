using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RowCycle.Domain.Entities;

namespace RowCycle.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasUserForeignKey(x => x.UserId, DeleteBehavior.SetNull);

        builder.Property(x => x.Action).HasMaxLength(100);
        builder.Property(x => x.Entity).HasMaxLength(100);
        builder.Property(x => x.EntityId).HasMaxLength(100);
        builder.Property(x => x.Data).HasColumnType("jsonb");
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(x => x.CreatedAt).IsDescending();
        builder.HasIndex(x => new { x.Entity, x.EntityId });
    }
}
