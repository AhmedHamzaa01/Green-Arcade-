namespace RowCycle.Application.Dtos;

public sealed record AddToCartRequest(
    Guid VariantId,
    int Quantity);

public sealed record UpdateCartItemRequest(
    int Quantity);

public sealed record CartItemResponse(
    Guid VariantId,
    Guid ProductId,
    string ProductName,
    string VariantName,
    string? Sku,
    string? ImageUrl,
    decimal UnitPriceEgp,
    int? UnitPricePoints,
    int UnitRewardPoints,
    int Quantity,
    int Stock,
    decimal LineTotalEgp,
    int? LineTotalPoints);

public sealed record CartResponse(
    IReadOnlyList<CartItemResponse> Items,
    decimal TotalEgp,
    int TotalPoints,
    int TotalRewardPoints);

