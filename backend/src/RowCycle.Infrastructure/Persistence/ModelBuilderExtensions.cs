using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RowCycle.Infrastructure.Identity;

namespace RowCycle.Infrastructure.Persistence;

internal static class ModelBuilderExtensions
{
    /// <summary>Adds a FK from <paramref name="foreignKey"/> to asp_net_users without a navigation property.</summary>
    public static void HasUserForeignKey<T>(
        this EntityTypeBuilder<T> builder,
        Expression<Func<T, object?>> foreignKey,
        DeleteBehavior onDelete = DeleteBehavior.Restrict)
        where T : class
    {
        builder.HasOne<AppUser>().WithMany().HasForeignKey(foreignKey).OnDelete(onDelete);
    }
}
