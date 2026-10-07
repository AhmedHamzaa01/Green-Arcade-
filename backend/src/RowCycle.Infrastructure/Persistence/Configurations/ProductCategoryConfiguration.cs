using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RowCycle.Domain.Entities;

namespace RowCycle.Infrastructure.Persistence.Configurations;

internal sealed class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.ToTable("product_categories");
        builder.Property(x => x.Name).HasMaxLength(100);
        builder.Property(x => x.Slug).HasMaxLength(100);
        builder.HasIndex(x => x.Slug).IsUnique().HasFilter("deleted_at IS NULL");
    }
}
