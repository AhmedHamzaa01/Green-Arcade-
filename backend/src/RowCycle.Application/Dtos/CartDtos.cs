namespace RowCycle.Application.Dtos;

/// <summary>A cart line, with the product's current price and whether it can still be bought.</summary>
public sealed record CartItemResponse(
    Guid VariantId,
    Guid ProductId,
    string ProductSlug,
    string ProductName,
    string VariantName,
    string? ImageUrl,
    decimal UnitPriceEgp,
    int? UnitPricePoints,
    int UnitRewardPoints,
    int Quantity,
    decimal LineTotalEgp,
    bool IsAvailable,
    bool HasEnoughStock);

/// <summary>
/// The cart with current prices. <c>TotalPoints</c> is null when some item has no points price
/// (then <c>CanPayWithPoints</c> is false). <c>HasIssues</c> means checkout will be refused until fixed.
/// </summary>
public sealed record CartResponse(
    IReadOnlyList<CartItemResponse> Items,
    int ItemCount,
    decimal TotalEgp,
    int? TotalPoints,
    int TotalRewardPoints,
    bool CanPayWithPoints,
    bool HasIssues);

public sealed record AddCartItemRequest(Guid VariantId, int Quantity);

public sealed record UpdateCartItemRequest(int Quantity);
