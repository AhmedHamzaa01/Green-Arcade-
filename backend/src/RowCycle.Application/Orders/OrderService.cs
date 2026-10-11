using AutoMapper;
using FluentValidation;
using RowCycle.Application.Addresses;
using RowCycle.Application.Cart;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Application.Points;
using RowCycle.Domain.Entities;
using RowCycle.Domain.Enums;
using RowCycle.Domain.Rules;

namespace RowCycle.Application.Orders;

internal sealed class OrderService(
    IOrderRepository orders,
    ICartRepository carts,
    IAddressRepository addresses,
    IPointsService points,
    OrderWorkflow workflow,
    IUnitOfWork unitOfWork,
    IValidator<CheckoutRequest> checkoutValidator,
    IValidator<PageRequest> pageValidator,
    IMapper mapper,
    TimeProvider timeProvider) : IOrderService
{
    public async Task<OrderResponse> CheckoutAsync(Guid userId, CheckoutRequest request, CancellationToken cancellationToken = default)
    {
        await checkoutValidator.ValidateAndThrowAsync(request, cancellationToken);

        var orderId = Guid.CreateVersion7();
        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var cart = await carts.FindByUserAsync(userId, readOnly: true, ct);
            if (cart is null || cart.Items.Count == 0)
            {
                throw AppException.Validation("Your cart is empty.");
            }

            var address = (await addresses.ListAsync(userId, ct)).SingleOrDefault(a => a.Id == request.AddressId)
                ?? throw AppException.Validation("Address not found.", "Choose one of your saved addresses.");

            // 1. Lock the stock rows, then check each line against the locked (current) values.
            var variants = await orders.LockVariantsAsync(cart.Items.Select(i => i.VariantId).ToList(), ct);
            var order = new Order
            {
                Id = orderId,
                OrderNumber = await orders.NextOrderNumberAsync(ct),
                UserId = userId,
                PaymentMethod = request.PaymentMethod,
                Status = OrderStatus.Placed,
                ShippingAddress = new ShippingAddressSnapshot
                {
                    FullName = address.FullName,
                    Phone = address.Phone,
                    City = address.City,
                    Area = address.Area,
                    Street = address.Street,
                    Building = address.Building,
                    Notes = address.Notes,
                },
                CreatedAt = timeProvider.GetUtcNow(),
            };

            foreach (var line in cart.Items)
            {
                var variant = variants.SingleOrDefault(v => v.Id == line.VariantId);
                if (variant is null || !CartPricing.IsAvailable(variant))
                {
                    throw AppException.Conflict("Product not available.", $"\"{line.Variant?.Product?.Name}\" is no longer sold. Remove it from your cart.");
                }

                var product = variant.Product!;
                if (variant.Stock < line.Quantity)
                {
                    throw AppException.Conflict("Not enough stock.", $"Only {variant.Stock} left of \"{product.Name}\" ({variant.Name}).");
                }

                if (request.PaymentMethod == PaymentMethod.Points && product.PricePoints is null)
                {
                    throw AppException.Validation("Can't pay with points.", $"\"{product.Name}\" can only be bought with cash on delivery.");
                }

                // 2. Copy today's prices onto the order (FR-15), and take the stock.
                order.Items.Add(new OrderItem
                {
                    Id = Guid.CreateVersion7(),
                    OrderId = orderId,
                    VariantId = variant.Id,
                    ProductName = product.Name,
                    VariantName = variant.Name,
                    ImageUrl = CartPricing.MainImage(product),
                    UnitPriceEgp = CartPricing.UnitPrice(variant),
                    UnitPricePoints = product.PricePoints,
                    UnitRewardPoints = product.RewardPoints,
                    Quantity = line.Quantity,
                });
                variant.Stock -= line.Quantity;
            }

            order.TotalEgp = order.Items.Sum(i => i.UnitPriceEgp * i.Quantity);
            order.TotalPoints = request.PaymentMethod == PaymentMethod.Points ? order.Items.Sum(i => i.UnitPricePoints!.Value * i.Quantity) : 0;
            order.TotalRewardPoints = order.Items.Sum(i => i.UnitRewardPoints * i.Quantity);
            order.StatusHistory.Add(new OrderStatusHistory
            {
                Id = Guid.CreateVersion7(),
                OrderId = orderId,
                FromStatus = null,
                ToStatus = OrderStatus.Placed,
                ChangedBy = userId,
                ChangedAt = order.CreatedAt,
            });
            orders.Add(order);

            // 3. A points order spends the points in this same transaction; too few points undoes everything.
            if (order.TotalPoints > 0)
            {
                await points.RedeemAsync(userId, order.TotalPoints, orderId, $"Order {order.OrderNumber}", ct);
            }

            // 4. The cart is emptied only if everything above worked.
            await carts.ClearAsync(cart.Id, ct);
        }, cancellationToken);

        return await GetMineAsync(userId, orderId, cancellationToken);
    }

    public async Task<PagedResult<OrderSummaryResponse>> ListMineAsync(Guid userId, PageRequest page, CancellationToken cancellationToken = default)
    {
        await pageValidator.ValidateAndThrowAsync(page, cancellationToken);
        return (await orders.SearchAsync(userId, new OrderQuery(), page, cancellationToken)).Map(mapper.Map<OrderSummaryResponse>);
    }

    public async Task<OrderResponse> GetMineAsync(Guid userId, Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await orders.FindAsync(orderId, cancellationToken);
        return order is not null && order.UserId == userId
            ? mapper.Map<OrderResponse>(order)
            : throw AppException.NotFound("Order not found.");
    }

    public async Task<OrderResponse> CancelMineAsync(Guid userId, Guid orderId, CancellationToken cancellationToken = default)
    {
        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var order = await orders.LockAsync(orderId, ct);
            if (order is null || order.UserId != userId)
            {
                throw AppException.NotFound("Order not found.");
            }

            if (!OrderStatusFlow.MemberCanCancel(order.Status))
            {
                throw AppException.Conflict("Can't cancel this order.", "Orders can be cancelled until they are confirmed. Contact Row-Cycle for help.");
            }

            await workflow.MoveAsync(order, OrderStatus.Cancelled, userId, ct);
        }, cancellationToken);

        return await GetMineAsync(userId, orderId, cancellationToken);
    }
}

public sealed class CheckoutRequestValidator : AbstractValidator<CheckoutRequest>
{
    public CheckoutRequestValidator()
    {
        RuleFor(x => x.AddressId).NotEmpty();
        RuleFor(x => x.PaymentMethod).IsInEnum();
    }
}
