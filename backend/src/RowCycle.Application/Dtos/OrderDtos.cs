using RowCycle.Domain.Enums;

namespace RowCycle.Application.Dtos;

public sealed record ShippingAddressDto(
    string FullName,
    string? Phone,
    string City,
    string Area,
    string Street,
    string Building,
    string? Notes);

public sealed record CheckoutRequest(
    PaymentMethod PaymentMethod,
    ShippingAddressDto ShippingAddress);

public sealed record OrderItemResponse(
    Guid Id,
    Guid VariantId,
    string ProductName,
    decimal UnitPriceEgp,
    int? UnitPricePoints,
    int UnitRewardPoints,
    int Quantity,
    decimal LineTotalEgp,
    int? LineTotalPoints);

public sealed record OrderStatusHistoryResponse(
    Guid Id,
    OrderStatus? FromStatus,
    OrderStatus ToStatus,
    Guid? ChangedBy,
    DateTimeOffset ChangedAt);

public sealed record OrderSummaryResponse(
    Guid Id,
    string OrderNumber,
    PaymentMethod PaymentMethod,
    OrderStatus Status,
    decimal TotalEgp,
    int TotalPoints,
    int TotalRewardPoints,
    int ItemCount,
    DateTimeOffset CreatedAt);

public sealed record OrderDetailResponse(
    Guid Id,
    string OrderNumber,
    Guid UserId,
    PaymentMethod PaymentMethod,
    OrderStatus Status,
    decimal TotalEgp,
    int TotalPoints,
    int TotalRewardPoints,
    ShippingAddressDto ShippingAddress,
    DateTimeOffset CreatedAt,
    IReadOnlyList<OrderItemResponse> Items,
    IReadOnlyList<OrderStatusHistoryResponse> StatusHistory);

public sealed record UpdateOrderStatusRequest(
    OrderStatus Status);

