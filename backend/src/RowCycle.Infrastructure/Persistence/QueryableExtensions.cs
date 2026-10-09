using Microsoft.EntityFrameworkCore;
using RowCycle.Application.Common;

namespace RowCycle.Infrastructure.Persistence;

internal static class QueryableExtensions
{
    /// <summary>Counts the full query, then returns one page. The query must already be ordered.</summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query, PageRequest page, CancellationToken cancellationToken)
    {
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(page.Skip).Take(page.PageSize).ToListAsync(cancellationToken);
        return new PagedResult<T>(items, page.Page, page.PageSize, total);
    }
}
