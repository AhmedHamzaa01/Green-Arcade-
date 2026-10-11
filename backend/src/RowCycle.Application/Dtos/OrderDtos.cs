using RowCycle.Domain.Enums;

namespace RowCycle.Application.Dtos;

/// <summary>Place an order from the cart (FR-14). One payment method per order.</summary>
public sealed record CheckoutRequest(Guid AddressId, PaymentMethod PaymentMethod);

public sealed record ShippingAddressResponse(
    string FullName,
    string? Phone,
    string City,
    string Area,
    string Street,
    string Building,
    string? Notes);

/// <summary>A line as it was at checkout (FR-15): later price changes don't affect it.</summary>
public sealed record OrderItemResponse(
    Guid Id,
    Guid VariantId,
    string ProductName,
    string VariantName,
    string? ImageUrl,
    decimal UnitPriceEgp,
    int? UnitPricePoints,
    int UnitRewardPoints,
    int Quantity,
    decimal LineTotalEgp);

public sealed record OrderStatusHistoryResponse(OrderStatus? FromStatus, OrderStatus ToStatus, Guid? ChangedBy, DateTimeOffset ChangedAt);

public sealed record OrderResponse(
    Guid Id,
    string OrderNumber,
    Guid UserId,
    OrderStatus Status,
    PaymentMethod PaymentMethod,
    decimal TotalEgp,
    int TotalPoints,
    int TotalRewardPoints,
    bool RewardPending,
    bool CanCancel,
    IReadOnlyList<OrderStatus> NextStatuses,
    ShippingAddressResponse ShippingAddress,
    DateTimeOffset CreatedAt,
    IReadOnlyList<OrderItemResponse> Items,
    IReadOnlyList<OrderStatusHistoryResponse> StatusHistory);

/// <summary>A row in the order lists.</summary>
public sealed record OrderSummaryResponse(
    Guid Id,
    string OrderNumber,
    OrderStatus Status,
    PaymentMethod PaymentMethod,
    decimal TotalEgp,
    int TotalPoints,
    int TotalRewardPoints,
    int ItemCount,
    string RecipientName,
    string City,
    DateTimeOffset CreatedAt);

/// <summary>Filters for <c>GET /admin/orders</c>. Search matches the order number or the recipient's name.</summary>
public sealed record OrderQuery(OrderStatus? Status = null, string? Search = null);

public sealed record ChangeOrderStatusRequest(OrderStatus Status);
