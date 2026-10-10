using FluentValidation;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;

namespace RowCycle.Application.Catalog;

public sealed class ProductQueryValidator : AbstractValidator<ProductQuery>
{
    public ProductQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.Category).MaximumLength(100);
        RuleFor(x => x.Sort).IsInEnum();
    }
}

public sealed class AdminProductQueryValidator : AbstractValidator<AdminProductQuery>
{
    public AdminProductQueryValidator() => RuleFor(x => x.Search).MaximumLength(100);
}

public sealed class SaveProductCategoryRequestValidator : AbstractValidator<SaveProductCategoryRequest>
{
    public SaveProductCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Slug).MaximumLength(100).Matches(Slug.Pattern).When(x => !string.IsNullOrEmpty(x.Slug))
            .WithMessage("Use lowercase letters, digits and single dashes, e.g. \"gold-medals\".");
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class SaveProductRequestValidator : AbstractValidator<SaveProductRequest>
{
    public SaveProductRequestValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).MaximumLength(200).Matches(Slug.Pattern).When(x => !string.IsNullOrEmpty(x.Slug))
            .WithMessage("Use lowercase letters, digits and single dashes, e.g. \"gold-medal\".");
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.PriceEgp).GreaterThanOrEqualTo(0).LessThanOrEqualTo(1_000_000).PrecisionScale(12, 2, ignoreTrailingZeros: true);
        RuleFor(x => x.PricePoints).GreaterThan(0).When(x => x.PricePoints is not null)
            .WithMessage("Points price must be above 0, or empty if the product can't be bought with points.");
        RuleFor(x => x.RewardPoints).GreaterThanOrEqualTo(0);
    }
}

public sealed class SaveProductVariantRequestValidator : AbstractValidator<SaveProductVariantRequest>
{
    public SaveProductVariantRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(64).Matches("^[A-Za-z0-9_-]+$")
            .WithMessage("Use letters, digits, dashes and underscores only.");
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PriceOverride).GreaterThanOrEqualTo(0).LessThanOrEqualTo(1_000_000)
            .PrecisionScale(12, 2, ignoreTrailingZeros: true).When(x => x.PriceOverride is not null);
    }
}

public sealed class ReorderImagesRequestValidator : AbstractValidator<ReorderImagesRequest>
{
    public ReorderImagesRequestValidator() => RuleFor(x => x.ImageIds).NotEmpty();
}
