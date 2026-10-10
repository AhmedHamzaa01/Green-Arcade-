namespace RowCycle.Application.Dtos;

// ---------- Public store (F5, FR-13) ----------

public sealed record ProductCategoryResponse(Guid Id, string Name, string Slug, int SortOrder, bool IsActive);

public enum ProductSort
{
    Newest,
    PriceAsc,
    PriceDesc,
}

/// <summary>Filters for <c>GET /products</c>. All optional.</summary>
public sealed record ProductQuery(string? Category = null, string? Search = null, ProductSort Sort = ProductSort.Newest);

/// <summary>A product card in the store list.</summary>
public sealed record ProductListItem(
    Guid Id,
    string Slug,
    string Name,
    string CategoryName,
    string CategorySlug,
    decimal PriceEgp,
    int? PricePoints,
    int RewardPoints,
    string? ImageUrl,
    bool InStock);

public sealed record ProductImageResponse(Guid Id, string Url, int SortOrder);

/// <summary>A variant as shoppers see it: no SKU, no exact stock count.</summary>
public sealed record ProductVariantResponse(Guid Id, string Name, decimal PriceEgp, bool InStock);

public sealed record ProductDetailResponse(
    Guid Id,
    string Slug,
    string Name,
    string Description,
    string CategoryName,
    string CategorySlug,
    decimal PriceEgp,
    int? PricePoints,
    int RewardPoints,
    bool InStock,
    IReadOnlyList<ProductVariantResponse> Variants,
    IReadOnlyList<ProductImageResponse> Images);

// ---------- Admin (StoreManager, Admin) ----------

public sealed record SaveProductCategoryRequest(string Name, string? Slug, int SortOrder, bool IsActive);

/// <summary>Filters for <c>GET /admin/products</c>. Includes inactive products.</summary>
public sealed record AdminProductQuery(string? Search = null, Guid? CategoryId = null, bool? IsActive = null);

public sealed record AdminProductListItem(
    Guid Id,
    string Slug,
    string Name,
    string CategoryName,
    decimal PriceEgp,
    int? PricePoints,
    int RewardPoints,
    bool IsActive,
    int TotalStock,
    int VariantCount,
    string? ImageUrl,
    DateTimeOffset CreatedAt);

public sealed record AdminProductVariantResponse(Guid Id, string Name, string Sku, int Stock, decimal? PriceOverride);

public sealed record AdminProductResponse(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Slug,
    string Name,
    string Description,
    decimal PriceEgp,
    int? PricePoints,
    int RewardPoints,
    bool IsActive,
    DateTimeOffset CreatedAt,
    IReadOnlyList<AdminProductVariantResponse> Variants,
    IReadOnlyList<ProductImageResponse> Images);

/// <summary>Create or edit a product. Slug is made from the name when empty.</summary>
public sealed record SaveProductRequest(
    Guid CategoryId,
    string Name,
    string? Slug,
    string Description,
    decimal PriceEgp,
    int? PricePoints,
    int RewardPoints,
    bool IsActive);

/// <summary>Variant such as a size. <c>PriceOverride</c> replaces the product's EGP price for this variant.</summary>
public sealed record SaveProductVariantRequest(string Name, string Sku, int Stock, decimal? PriceOverride);

/// <summary>All of the product's image ids, in the new display order.</summary>
public sealed record ReorderImagesRequest(IReadOnlyList<Guid> ImageIds);

/// <summary>An uploaded file, without any web types, so Application stays free of ASP.NET.</summary>
public sealed record FileUpload(Stream Content, string FileName, long Length);
