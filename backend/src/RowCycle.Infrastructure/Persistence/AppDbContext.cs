using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RowCycle.Domain.Common;
using RowCycle.Domain.Entities;
using RowCycle.Domain.Enums;
using RowCycle.Infrastructure.Identity;

namespace RowCycle.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, AppRole, Guid>(options)
{
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<PointsLedgerEntry> PointsLedger => Set<PointsLedgerEntry>();
    public DbSet<SubmissionCategory> SubmissionCategories => Set<SubmissionCategory>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<Badge> Badges => Set<Badge>();
    public DbSet<UserBadge> UserBadges => Set<UserBadge>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistory => Set<OrderStatusHistory>();
    public DbSet<Setting> Settings => Set<Setting>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Identity sets PascalCase table names explicitly; rename them to match SRS §4 (asp_net_users, ...).
        builder.Entity<AppUser>().ToTable("asp_net_users");
        builder.Entity<AppUser>().Property(u => u.IsActive).HasDefaultValue(true).HasSentinel(true);
        builder.Entity<AppRole>().ToTable("asp_net_roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("asp_net_user_roles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("asp_net_user_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("asp_net_user_logins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("asp_net_user_tokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("asp_net_role_claims");

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        SeedData.Apply(builder);
        ApplySoftDeleteFilters(builder);
    }

    /// <summary>Hides soft-deleted rows from every query. Use <c>IgnoreQueryFilters()</c> to see them (e.g. history views).</summary>
    private static void ApplySoftDeleteFilters(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes()
                     .Where(t => typeof(ISoftDeletable).IsAssignableFrom(t.ClrType)))
        {
            var entity = Expression.Parameter(entityType.ClrType, "e");
            var notDeleted = Expression.Lambda(
                Expression.Equal(
                    Expression.Property(entity, nameof(ISoftDeletable.DeletedAt)),
                    Expression.Constant(null, typeof(DateTimeOffset?))),
                entity);
            builder.Entity(entityType.ClrType).HasQueryFilter(notDeleted);
        }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Enums are stored as text so rows stay readable in pgAdmin/DBeaver.
        configurationBuilder.Properties<PointsEntryType>().HaveConversion<string>().HaveMaxLength(16);
        configurationBuilder.Properties<PointsSourceType>().HaveConversion<string>().HaveMaxLength(16);
        configurationBuilder.Properties<SubmissionStatus>().HaveConversion<string>().HaveMaxLength(16);
        configurationBuilder.Properties<OrderStatus>().HaveConversion<string>().HaveMaxLength(16);
        configurationBuilder.Properties<PaymentMethod>().HaveConversion<string>().HaveMaxLength(16);
        configurationBuilder.Properties<BadgeRuleType>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<ProductType>().HaveConversion<string>().HaveMaxLength(16);

        // Money is numeric(12,2) EGP; weight_kg overrides this in its configuration.
        configurationBuilder.Properties<decimal>().HavePrecision(12, 2);
    }
}
