using FluentValidation;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Domain.Entities;

namespace RowCycle.Application.Orders;

internal sealed class CartService(
    ICartRepository cartRepository,
    IUnitOfWork unitOfWork,
    IValidator<AddToCartRequest> addValidator,
    IValidator<UpdateCartItemRequest> updateValidator) : ICartService
{
    public async Task<CartResponse> GetCartAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cart = await cartRepository.GetByUserIdAsync(userId, cancellationToken);
        return BuildResponse(cart);
    }

    public async Task<CartResponse> AddItemAsync(Guid userId, AddToCartRequest request, CancellationToken cancellationToken = default)
    {
        await addValidator.ValidateAndThrowAsync(request, cancellationToken);

        var variant = await cartRepository.FindVariantWithProductAsync(request.VariantId, cancellationToken);
        if (variant is null || variant.Product is null || !variant.Product.IsActive)
        {
            throw new AppException(ErrorKind.NotFound, "Variant not found", "Product variant does not exist or is inactive.");
        }

        var cart = await cartRepository.GetOrCreateByUserIdAsync(userId, cancellationToken);
        var existing = cart.Items.FirstOrDefault(i => i.VariantId == request.VariantId);
        var targetQuantity = (existing?.Quantity ?? 0) + request.Quantity;

        if (targetQuantity > variant.Stock)
        {
            throw new AppException(ErrorKind.Conflict, "Insufficient stock",
                $"Cannot add {request.Quantity}. Total requested quantity ({targetQuantity}) exceeds available stock ({variant.Stock}).");
        }

        if (existing is not null)
        {
            existing.Quantity = targetQuantity;
        }
        else
        {
            var newItem = new CartItem
            {
                CartId = cart.Id,
                VariantId = variant.Id,
                Variant = variant,
                Quantity = request.Quantity,
            };
            cart.Items.Add(newItem);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return BuildResponse(cart);
    }

    public async Task<CartResponse> UpdateItemAsync(Guid userId, Guid variantId, UpdateCartItemRequest request, CancellationToken cancellationToken = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var cart = await cartRepository.GetByUserIdAsync(userId, cancellationToken);
        if (cart is null)
        {
            throw new AppException(ErrorKind.NotFound, "Cart not found", "Cart is empty.");
        }

        var item = cart.Items.FirstOrDefault(i => i.VariantId == variantId);
        if (item is null)
        {
            throw new AppException(ErrorKind.NotFound, "Item not found", "The requested item is not in your cart.");
        }

        if (request.Quantity <= 0)
        {
            cartRepository.RemoveItem(item);
            cart.Items.Remove(item);
        }
        else
        {
            if (item.Variant is not null && request.Quantity > item.Variant.Stock)
            {
                throw new AppException(ErrorKind.Conflict, "Insufficient stock",
                    $"Requested quantity ({request.Quantity}) exceeds available stock ({item.Variant.Stock}).");
            }

            item.Quantity = request.Quantity;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return BuildResponse(cart);
    }

    public async Task<CartResponse> RemoveItemAsync(Guid userId, Guid variantId, CancellationToken cancellationToken = default)
    {
        var cart = await cartRepository.GetByUserIdAsync(userId, cancellationToken);
        if (cart is null)
        {
            return new CartResponse([], 0, 0, 0);
        }

        var item = cart.Items.FirstOrDefault(i => i.VariantId == variantId);
        if (item is not null)
        {
            cartRepository.RemoveItem(item);
            cart.Items.Remove(item);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return BuildResponse(cart);
    }

    public async Task ClearCartAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cart = await cartRepository.GetByUserIdAsync(userId, cancellationToken);
        if (cart is not null && cart.Items.Count > 0)
        {
            cartRepository.ClearCart(cart);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private static CartResponse BuildResponse(Cart? cart)
    {
        if (cart is null || cart.Items.Count == 0)
        {
            return new CartResponse([], 0, 0, 0);
        }

        var activeItems = cart.Items
            .Where(i => i.Variant is not null && i.Variant.Product is not null && i.Variant.Product.IsActive)
            .Select(i =>
            {
                var variant = i.Variant!;
                var product = variant.Product!;
                var unitPriceEgp = variant.PriceOverride ?? product.PriceEgp;
                var unitPricePoints = product.PricePoints;
                var lineTotalEgp = unitPriceEgp * i.Quantity;
                var lineTotalPoints = unitPricePoints.HasValue ? unitPricePoints.Value * i.Quantity : (int?)null;
                var mainImage = product.Images.OrderBy(img => img.SortOrder).FirstOrDefault()?.Url;

                return new CartItemResponse(
                    variant.Id,
                    product.Id,
                    product.Name,
                    variant.Name,
                    variant.Sku,
                    mainImage,
                    unitPriceEgp,
                    unitPricePoints,
                    product.RewardPoints,
                    i.Quantity,
                    variant.Stock,
                    lineTotalEgp,
                    lineTotalPoints);
            })
            .ToList();

        var totalEgp = activeItems.Sum(x => x.LineTotalEgp);
        var allHavePoints = activeItems.Count > 0 && activeItems.All(x => x.UnitPricePoints.HasValue);
        var totalPoints = allHavePoints ? activeItems.Sum(x => x.LineTotalPoints ?? 0) : 0;
        var totalRewardPoints = activeItems.Sum(x => x.UnitRewardPoints * x.Quantity);

        return new CartResponse(activeItems, totalEgp, totalPoints, totalRewardPoints);
    }
}

