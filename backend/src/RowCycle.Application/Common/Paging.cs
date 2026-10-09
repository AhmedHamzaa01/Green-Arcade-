using FluentValidation;

namespace RowCycle.Application.Common;

/// <summary>Page number (from 1) and size for list endpoints.</summary>
public sealed record PageRequest(int Page = 1, int PageSize = PageRequest.DefaultPageSize)
{
    public const int DefaultPageSize = 10 ;
    public const int MaxPageSize = 100;

    public int Skip => (Page - 1) * PageSize;
}

/// <summary>The standard list shape: <c>{ items, page, pageSize, total }</c> (SRS rule).</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total)
{
    public PagedResult<TOut> Map<TOut>(Func<T, TOut> map) => new(Items.Select(map).ToList(), Page, PageSize, Total);
}

public sealed class PageRequestValidator : AbstractValidator<PageRequest>
{
    public PageRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, PageRequest.MaxPageSize);
    }
}
