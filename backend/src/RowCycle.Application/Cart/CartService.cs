using AutoMapper;
using FluentValidation;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Domain.Entities;

namespace RowCycle.Application.Cart;

internal sealed class CartService(
    ICartRepository carts,
    IUnitOfWork unitOfWork,
    IValidator<AddCartItemRequest> addValidator,
    IValidator<UpdateCartItemRequest> updateValidator,
    IMapper mapper) : ICartService
{
    public const int MaxQuantity = 99;

    public async Task<CartResponse> GetAsync(Guid userId, CancellationToken cancellationToken = default) =>
        ToResponse(await carts.FindByUserAsync(userId, readOnly: true, cancellationToken));

    public async Task<CartResponse> AddItemAsync(Guid userId, AddCartItemRequest request, CancellationToken cancellationToken = default)
    {
        await addValidator.ValidateAndThrowAsync(request, cancellationToken);

        var variant = await carts.FindVariantAsync(request.VariantId, cancellationToken);
        if (variant is null || !CartPricing.IsAvailable(variant))
        {
            throw AppException.Conflict("Product not available.", "It may have been removed from the store.");
        }

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var cart = await GetOrCreateAsync(userId, ct);
            var line = cart.Items.SingleOrDefault(i => i.VariantId == request.VariantId);
            var quantity = (line?.Quantity ?? 0) + request.Quantity;
            EnsureStock(variant, quantity);

            if (line is null)
            {
                carts.AddItem(new CartItem { CartId = cart.Id, VariantId = request.VariantId, Quantity = quantity });
            }
            else
            {
                line.Quantity = quantity;
            }
        }, cancellationToken);

        return await GetAsync(userId, cancellationToken);
    }

    public async Task<CartResponse> UpdateItemAsync(
        Guid userId, Guid variantId, UpdateCartItemRequest request, CancellationToken cancellationToken = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var line = await FindLineAsync(userId, variantId, ct);
            // Lowering the quantity is always fine; raising it needs stock.
            if (request.Quantity > line.Quantity && line.Variant is { } variant)
            {
                EnsureStock(variant, request.Quantity);
            }

            line.Quantity = request.Quantity;
        }, cancellationToken);

        return await GetAsync(userId, cancellationToken);
    }

    public async Task<CartResponse> RemoveItemAsync(Guid userId, Guid variantId, CancellationToken cancellationToken = default)
    {
        await unitOfWork.ExecuteInTransactionAsync(async ct => carts.RemoveItem(await FindLineAsync(userId, variantId, ct)), cancellationToken);
        return await GetAsync(userId, cancellationToken);
    }

    public async Task<CartResponse> ClearAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (await carts.FindByUserAsync(userId, readOnly: true, cancellationToken) is { } cart)
        {
            await unitOfWork.ExecuteInTransactionAsync(ct => carts.ClearAsync(cart.Id, ct), cancellationToken);
        }

        return await GetAsync(userId, cancellationToken);
    }

    private async Task<Domain.Entities.Cart> GetOrCreateAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (await carts.FindByUserAsync(userId, readOnly: false, cancellationToken) is { } existing)
        {
            return existing;
        }

        var cart = new Domain.Entities.Cart { Id = Guid.CreateVersion7(), UserId = userId };
        carts.Add(cart);
        return cart;
    }

    private async Task<CartItem> FindLineAsync(Guid userId, Guid variantId, CancellationToken cancellationToken)
    {
        var cart = await carts.FindByUserAsync(userId, readOnly: false, cancellationToken);
        return cart?.Items.SingleOrDefault(i => i.VariantId == variantId) ?? throw AppException.NotFound("Item not in your cart.");
    }

    private static void EnsureStock(ProductVariant variant, int quantity)
    {
        if (quantity > MaxQuantity)
        {
            throw AppException.Validation("Quantity too large.", $"You can order up to {MaxQuantity} of one item.");
        }

        if (variant.Stock < quantity)
        {
            throw AppException.Conflict("Not enough stock.",
                variant.Stock == 0 ? "This item is out of stock." : $"Only {variant.Stock} left.");
        }
    }

    private CartResponse ToResponse(Domain.Entities.Cart? cart)
    {
        var items = mapper.Map<List<CartItemResponse>>(cart?.Items.OrderBy(i => i.Variant!.Product?.Name) ?? Enumerable.Empty<CartItem>());
        var canPayWithPoints = items.Count > 0 && items.All(i => i.UnitPricePoints is not null);

        return new CartResponse(
            items,
            items.Sum(i => i.Quantity),
            items.Sum(i => i.LineTotalEgp),
            canPayWithPoints ? items.Sum(i => i.UnitPricePoints!.Value * i.Quantity) : null,
            items.Sum(i => i.UnitRewardPoints * i.Quantity),
            canPayWithPoints,
            items.Any(i => !i.IsAvailable || !i.HasEnoughStock));
    }
}

/// <summary>Price and availability rules shared by the cart and checkout.</summary>
internal static class CartPricing
{
    /// <summary>Buyable: the variant and its product exist, and the product and its category are active.</summary>
    public static bool IsAvailable(ProductVariant variant) =>
        variant.DeletedAt is null
        && variant.Product is { DeletedAt: null, IsActive: true, Category.IsActive: true };

    public static decimal UnitPrice(ProductVariant variant) => variant.PriceOverride ?? variant.Product!.PriceEgp;

    public static string? MainImage(Product product) =>
        product.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault();
}

public sealed class AddCartItemRequestValidator : AbstractValidator<AddCartItemRequest>
{
    public AddCartItemRequestValidator()
    {
        RuleFor(x => x.VariantId).NotEmpty();
        RuleFor(x => x.Quantity).InclusiveBetween(1, CartService.MaxQuantity);
    }
}

public sealed class UpdateCartItemRequestValidator : AbstractValidator<UpdateCartItemRequest>
{
    public UpdateCartItemRequestValidator() => RuleFor(x => x.Quantity).InclusiveBetween(1, CartService.MaxQuantity);
}
