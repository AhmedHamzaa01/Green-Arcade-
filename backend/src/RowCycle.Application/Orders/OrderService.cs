using AutoMapper;
using FluentValidation;
using RowCycle.Application.Audit;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Application.Points;
using RowCycle.Domain.Entities;
using RowCycle.Domain.Enums;

namespace RowCycle.Application.Orders;

internal sealed class OrderService(
    IOrderRepository orderRepository,
    ICartRepository cartRepository,
    IPointsService pointsService,
    IAuditLogService audit,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IValidator<CheckoutRequest> checkoutValidator,
    IValidator<UpdateOrderStatusRequest> updateStatusValidator,
    TimeProvider timeProvider) : IOrderService
{
    public async Task<OrderDetailResponse> CheckoutAsync(
        Guid userId, CheckoutRequest request, CancellationToken cancellationToken = default)
    {
        await checkoutValidator.ValidateAndThrowAsync(request, cancellationToken);

        var cart = await cartRepository.GetByUserIdAsync(userId, cancellationToken);
        if (cart is null || cart.Items.Count == 0)
        {
            throw new AppException(ErrorKind.Conflict, "Cart is empty", "Your cart is empty. Add items before checking out.");
        }

        var activeItems = cart.Items
            .Where(i => i.Variant is not null && i.Variant.Product is not null && i.Variant.Product.IsActive)
            .ToList();

        if (activeItems.Count != cart.Items.Count)
        {
            throw new AppException(ErrorKind.Conflict, "Cart has unavailable items",
                "Some items in your cart are no longer active or available.");
        }

        // Validate stock
        foreach (var item in activeItems)
        {
            if (item.Variant!.Stock < item.Quantity)
            {
                throw new AppException(ErrorKind.Conflict, "Insufficient stock",
                    $"Not enough stock for '{item.Variant.Product!.Name} - {item.Variant.Name}'. Available: {item.Variant.Stock}, requested: {item.Quantity}.");
            }
        }

        // Validate points payment eligibility
        if (request.PaymentMethod == PaymentMethod.Points)
        {
            foreach (var item in activeItems)
            {
                if (!item.Variant!.Product!.PricePoints.HasValue)
                {
                    throw new AppException(ErrorKind.Conflict, "Item cannot be purchased with points",
                        $"Product '{item.Variant.Product.Name}' cannot be purchased with points.");
                }
            }
        }

        var totalEgp = request.PaymentMethod == PaymentMethod.Cod
            ? activeItems.Sum(i => i.Quantity * (i.Variant!.PriceOverride ?? i.Variant!.Product!.PriceEgp))
            : 0m;

        var totalPoints = request.PaymentMethod == PaymentMethod.Points
            ? activeItems.Sum(i => i.Quantity * i.Variant!.Product!.PricePoints!.Value)
            : 0;

        var totalRewardPoints = activeItems.Sum(i => i.Quantity * i.Variant!.Product!.RewardPoints);

        string orderNumber;
        do
        {
            orderNumber = $"RC-{timeProvider.GetUtcNow():yyMMdd}-{Random.Shared.Next(1000, 9999)}";
        } while (await orderRepository.OrderNumberExistsAsync(orderNumber, cancellationToken));

        var now = timeProvider.GetUtcNow();
        var order = new Order
        {
            Id = Guid.CreateVersion7(),
            OrderNumber = orderNumber,
            UserId = userId,
            PaymentMethod = request.PaymentMethod,
            Status = OrderStatus.Placed,
            TotalEgp = totalEgp,
            TotalPoints = totalPoints,
            TotalRewardPoints = totalRewardPoints,
            ShippingAddress = new ShippingAddressSnapshot
            {
                FullName = request.ShippingAddress.FullName,
                Phone = request.ShippingAddress.Phone,
                City = request.ShippingAddress.City,
                Area = request.ShippingAddress.Area,
                Street = request.ShippingAddress.Street,
                Building = request.ShippingAddress.Building,
                Notes = request.ShippingAddress.Notes,
            },
            CreatedAt = now,
        };

        foreach (var item in activeItems)
        {
            var variant = item.Variant!;
            var product = variant.Product!;
            order.Items.Add(new OrderItem
            {
                Id = Guid.CreateVersion7(),
                OrderId = order.Id,
                VariantId = variant.Id,
                ProductName = product.Name,
                UnitPriceEgp = variant.PriceOverride ?? product.PriceEgp,
                UnitPricePoints = product.PricePoints,
                UnitRewardPoints = product.RewardPoints,
                Quantity = item.Quantity,
            });
        }

        order.StatusHistory.Add(new OrderStatusHistory
        {
            Id = Guid.CreateVersion7(),
            OrderId = order.Id,
            FromStatus = null,
            ToStatus = OrderStatus.Placed,
            ChangedBy = userId,
            ChangedAt = now,
        });

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            // 1. Decrement stock
            foreach (var item in activeItems)
            {
                item.Variant!.Stock -= item.Quantity;
            }

            // 2. Add order
            orderRepository.Add(order);

            // 3. For Points payment: redeem ledger row under row lock (joins transaction)
            if (request.PaymentMethod == PaymentMethod.Points)
            {
                await pointsService.RedeemAsync(userId, totalPoints, order.Id, $"Order {order.OrderNumber}", ct);
            }

            // 4. Clear cart
            cartRepository.ClearCart(cart);
        }, cancellationToken);

        return mapper.Map<OrderDetailResponse>(order);
    }

    public async Task<PagedResult<OrderSummaryResponse>> GetMyOrdersAsync(
        Guid userId, PageRequest page, CancellationToken cancellationToken = default)
    {
        var result = await orderRepository.ListByUserAsync(userId, page, cancellationToken);
        var mapped = mapper.Map<List<OrderSummaryResponse>>(result.Items);
        return new PagedResult<OrderSummaryResponse>(mapped, result.Page, result.PageSize, result.Total);
    }

    public async Task<OrderDetailResponse> GetMyOrderByIdAsync(
        Guid userId, Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(orderId, includeDetails: true, cancellationToken);
        if (order is null || order.UserId != userId)
        {
            throw new AppException(ErrorKind.NotFound, "Order not found", "Order was not found.");
        }

        return mapper.Map<OrderDetailResponse>(order);
    }

    public async Task<OrderDetailResponse> CancelMyOrderAsync(
        Guid userId, Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(orderId, includeDetails: true, cancellationToken);
        if (order is null || order.UserId != userId)
        {
            throw new AppException(ErrorKind.NotFound, "Order not found", "Order was not found.");
        }

        if (order.Status != OrderStatus.Placed)
        {
            throw new AppException(ErrorKind.Conflict, "Order cannot be cancelled",
                "Only orders in 'Placed' status can be cancelled by the customer.");
        }

        var now = timeProvider.GetUtcNow();

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            // Restore stock
            foreach (var item in order.Items)
            {
                var variant = await orderRepository.FindVariantAsync(item.VariantId, ct);
                if (variant is not null)
                {
                    variant.Stock += item.Quantity;
                }
            }

            // Reverse points if Points order
            if (order.PaymentMethod == PaymentMethod.Points)
            {
                await pointsService.ReverseAsync(
                    PointsSourceType.Order, order.Id, $"Order {order.OrderNumber} cancelled by customer", userId, ct);
            }

            order.Status = OrderStatus.Cancelled;
            order.StatusHistory.Add(new OrderStatusHistory
            {
                Id = Guid.CreateVersion7(),
                OrderId = order.Id,
                FromStatus = OrderStatus.Placed,
                ToStatus = OrderStatus.Cancelled,
                ChangedBy = userId,
                ChangedAt = now,
            });
        }, cancellationToken);

        return mapper.Map<OrderDetailResponse>(order);
    }

    public async Task<PagedResult<OrderSummaryResponse>> GetAdminOrdersAsync(
        OrderStatus? status, PageRequest page, CancellationToken cancellationToken = default)
    {
        var result = await orderRepository.ListAdminAsync(status, page, cancellationToken);
        var mapped = mapper.Map<List<OrderSummaryResponse>>(result.Items);
        return new PagedResult<OrderSummaryResponse>(mapped, result.Page, result.PageSize, result.Total);
    }

    public async Task<OrderDetailResponse> GetAdminOrderByIdAsync(
        Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(orderId, includeDetails: true, cancellationToken);
        if (order is null)
        {
            throw new AppException(ErrorKind.NotFound, "Order not found", "Order was not found.");
        }

        return mapper.Map<OrderDetailResponse>(order);
    }

    public async Task<OrderDetailResponse> UpdateOrderStatusAsync(
        Guid orderId, Guid staffId, UpdateOrderStatusRequest request, CancellationToken cancellationToken = default)
    {
        await updateStatusValidator.ValidateAndThrowAsync(request, cancellationToken);

        var order = await orderRepository.GetByIdAsync(orderId, includeDetails: true, cancellationToken);
        if (order is null)
        {
            throw new AppException(ErrorKind.NotFound, "Order not found", "Order was not found.");
        }

        // FR-16: Valid status transitions
        var isValid = (order.Status, request.Status) switch
        {
            (OrderStatus.Placed, OrderStatus.Confirmed) => true,
            (OrderStatus.Placed, OrderStatus.Cancelled) => true,
            (OrderStatus.Confirmed, OrderStatus.Shipped) => true,
            (OrderStatus.Confirmed, OrderStatus.Cancelled) => true,
            (OrderStatus.Shipped, OrderStatus.Delivered) => true,
            _ => false,
        };

        if (!isValid)
        {
            throw new AppException(ErrorKind.Conflict, "Invalid status transition",
                $"Cannot transition order from {order.Status} to {request.Status}.");
        }

        var oldStatus = order.Status;
        var now = timeProvider.GetUtcNow();

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            // Transition to Cancelled: restore stock & reverse points
            if (request.Status == OrderStatus.Cancelled)
            {
                foreach (var item in order.Items)
                {
                    var variant = await orderRepository.FindVariantAsync(item.VariantId, ct);
                    if (variant is not null)
                    {
                        variant.Stock += item.Quantity;
                    }
                }

                if (order.PaymentMethod == PaymentMethod.Points)
                {
                    await pointsService.ReverseAsync(
                        PointsSourceType.Order, order.Id, $"Order {order.OrderNumber} cancelled by staff", staffId, ct);
                }
            }

            // Transition to Delivered: write Earn ledger row for product reward points (FR-18)
            if (request.Status == OrderStatus.Delivered && order.TotalRewardPoints > 0)
            {
                await pointsService.EarnAsync(
                    order.UserId,
                    order.TotalRewardPoints,
                    PointsSourceType.Order,
                    order.Id,
                    $"Reward points for order {order.OrderNumber}",
                    staffId,
                    ct);
            }

            order.Status = request.Status;
            order.StatusHistory.Add(new OrderStatusHistory
            {
                Id = Guid.CreateVersion7(),
                OrderId = order.Id,
                FromStatus = oldStatus,
                ToStatus = request.Status,
                ChangedBy = staffId,
                ChangedAt = now,
            });

            audit.Record(staffId, AuditActions.OrderStatusUpdate, "order", order.Id.ToString(), new
            {
                orderNumber = order.OrderNumber,
                fromStatus = oldStatus.ToString(),
                toStatus = request.Status.ToString(),
            });
        }, cancellationToken);

        return mapper.Map<OrderDetailResponse>(order);
    }
}

