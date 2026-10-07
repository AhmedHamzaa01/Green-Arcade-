using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RowCycle.Domain.Entities;

namespace RowCycle.Infrastructure.Persistence.Configurations;

internal sealed class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("addresses");
        builder.Property(x => x.City).HasMaxLength(100);
        builder.Property(x => x.Area).HasMaxLength(100);
        builder.Property(x => x.Street).HasMaxLength(200);
        builder.Property(x => x.Building).HasMaxLength(100);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.HasIndex(x => x.UserId);
    }
}
