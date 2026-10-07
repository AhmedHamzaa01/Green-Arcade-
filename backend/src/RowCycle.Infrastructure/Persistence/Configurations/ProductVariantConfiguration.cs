using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RowCycle.Domain.Entities;

namespace RowCycle.Infrastructure.Persistence.Configurations;

internal sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("product_variants", t =>
        {
            t.HasCheckConstraint("ck_product_variants_stock_non_negative", "stock >= 0");
            t.HasCheckConstraint("ck_product_variants_price_override_non_negative", "price_override IS NULL OR price_override >= 0");
        });
        builder.Property(x => x.Name).HasMaxLength(100);
        builder.Property(x => x.Sku).HasMaxLength(64);
        builder.HasIndex(x => x.Sku).IsUnique().HasFilter("deleted_at IS NULL");
    }
}
