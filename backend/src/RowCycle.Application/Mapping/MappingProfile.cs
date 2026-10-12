using System.Text.Json;
using AutoMapper;
using RowCycle.Application.Auth;
using RowCycle.Application.Dtos;
using RowCycle.Domain.Entities;
using RowCycle.Domain.Enums;
using RowCycle.Domain.Rules;

namespace RowCycle.Application.Mapping;

/// <summary>
/// Every entity → DTO mapping in the app, grouped by feature. <c>MappingTests</c> checks that each DTO field has a source.
/// </summary>
internal sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        MapAuth();
        MapPoints();
        MapAudit();
        MapCatalog();
        MapOrders();
        MapSubmissions();
    }

    private void MapAuth()
    {
        CreateMap<MeSource, MeResponse>()
            .ForCtorParam(nameof(MeResponse.Id), o => o.MapFrom(s => s.Account.Id))
            .ForCtorParam(nameof(MeResponse.Email), o => o.MapFrom(s => s.Account.Email))
            .ForCtorParam(nameof(MeResponse.EmailVerified), o => o.MapFrom(s => s.Account.EmailConfirmed))
            .ForCtorParam(nameof(MeResponse.FullName), o => o.MapFrom(s => s.Profile.FullName))
            .ForCtorParam(nameof(MeResponse.PhotoUrl), o => o.MapFrom(s => s.Profile.PhotoUrl))
            .ForCtorParam(nameof(MeResponse.Phone), o => o.MapFrom(s => s.Profile.Phone))
            .ForCtorParam(nameof(MeResponse.PointsBalance), o => o.MapFrom(s => s.Profile.PointsBalance))
            .ForCtorParam(nameof(MeResponse.Roles), o => o.MapFrom(s => s.Roles.Order().ToList()));
    }

    private void MapPoints()
    {
        CreateMap<PointsLedgerEntry, PointsEntryResponse>();
    }

    private void MapAudit()
    {
        CreateMap<AuditLog, AuditLogResponse>()
            .ForCtorParam(nameof(AuditLogResponse.Data), o => o.MapFrom(s => ParseJson(s.Data)));
    }

    private void MapCatalog()
    {
        CreateMap<ProductCategory, ProductCategoryResponse>();
        CreateMap<ProductImage, ProductImageResponse>();
        CreateMap<ProductVariant, AdminProductVariantResponse>();

        // Shoppers see the variant's own price (or the product's) and whether it's in stock, not the stock count.
        CreateMap<ProductVariant, ProductVariantResponse>()
            .ForCtorParam(nameof(ProductVariantResponse.PriceEgp), o => o.MapFrom(v => v.PriceOverride ?? v.Product!.PriceEgp))
            .ForCtorParam(nameof(ProductVariantResponse.InStock), o => o.MapFrom(v => v.Stock > 0));

        CreateMap<Product, ProductListItem>()
            .ForCtorParam(nameof(ProductListItem.CategoryName), o => o.MapFrom(p => p.Category!.Name))
            .ForCtorParam(nameof(ProductListItem.CategorySlug), o => o.MapFrom(p => p.Category!.Slug))
            .ForCtorParam(nameof(ProductListItem.ImageUrl), o => o.MapFrom(p => MainImageUrl(p)))
            .ForCtorParam(nameof(ProductListItem.InStock), o => o.MapFrom(p => p.Variants.Any(v => v.Stock > 0)));

        CreateMap<Product, ProductDetailResponse>()
            .ForCtorParam(nameof(ProductDetailResponse.CategoryName), o => o.MapFrom(p => p.Category!.Name))
            .ForCtorParam(nameof(ProductDetailResponse.CategorySlug), o => o.MapFrom(p => p.Category!.Slug))
            .ForCtorParam(nameof(ProductDetailResponse.InStock), o => o.MapFrom(p => p.Variants.Any(v => v.Stock > 0)))
            .ForCtorParam(nameof(ProductDetailResponse.Variants), o => o.MapFrom(p => p.Variants.OrderBy(v => v.Name)))
            .ForCtorParam(nameof(ProductDetailResponse.Images), o => o.MapFrom(p => p.Images.OrderBy(i => i.SortOrder)));

        CreateMap<Product, AdminProductListItem>()
            .ForCtorParam(nameof(AdminProductListItem.CategoryName), o => o.MapFrom(p => p.Category!.Name))
            .ForCtorParam(nameof(AdminProductListItem.TotalStock), o => o.MapFrom(p => p.Variants.Sum(v => v.Stock)))
            .ForCtorParam(nameof(AdminProductListItem.VariantCount), o => o.MapFrom(p => p.Variants.Count))
            .ForCtorParam(nameof(AdminProductListItem.ImageUrl), o => o.MapFrom(p => MainImageUrl(p)));

        CreateMap<Product, AdminProductResponse>()
            .ForCtorParam(nameof(AdminProductResponse.CategoryName), o => o.MapFrom(p => p.Category!.Name))
            .ForCtorParam(nameof(AdminProductResponse.Variants), o => o.MapFrom(p => p.Variants.OrderBy(v => v.Name)))
            .ForCtorParam(nameof(AdminProductResponse.Images), o => o.MapFrom(p => p.Images.OrderBy(i => i.SortOrder)));
    }

    private void MapOrders()
    {
        CreateMap<Address, AddressResponse>();
        CreateMap<ShippingAddressSnapshot, ShippingAddressResponse>();
        CreateMap<OrderStatusHistory, OrderStatusHistoryResponse>();

        // Cart lines are priced from the product as it is today.
        CreateMap<CartItem, CartItemResponse>()
            .ForCtorParam(nameof(CartItemResponse.ProductId), o => o.MapFrom(i => i.Variant!.ProductId))
            .ForCtorParam(nameof(CartItemResponse.ProductSlug), o => o.MapFrom(i => i.Variant!.Product!.Slug))
            .ForCtorParam(nameof(CartItemResponse.ProductName), o => o.MapFrom(i => i.Variant!.Product!.Name))
            .ForCtorParam(nameof(CartItemResponse.VariantName), o => o.MapFrom(i => i.Variant!.Name))
            .ForCtorParam(nameof(CartItemResponse.ImageUrl), o => o.MapFrom(i => MainImageUrl(i.Variant!.Product!)))
            .ForCtorParam(nameof(CartItemResponse.UnitPriceEgp), o => o.MapFrom(i => i.Variant!.PriceOverride ?? i.Variant.Product!.PriceEgp))
            .ForCtorParam(nameof(CartItemResponse.UnitPricePoints), o => o.MapFrom(i => i.Variant!.Product!.PricePoints))
            .ForCtorParam(nameof(CartItemResponse.UnitRewardPoints), o => o.MapFrom(i => i.Variant!.Product!.RewardPoints))
            .ForCtorParam(nameof(CartItemResponse.LineTotalEgp), o => o.MapFrom(i => (i.Variant!.PriceOverride ?? i.Variant.Product!.PriceEgp) * i.Quantity))
            .ForCtorParam(nameof(CartItemResponse.IsAvailable), o => o.MapFrom(i => Cart.CartPricing.IsAvailable(i.Variant!)))
            .ForCtorParam(nameof(CartItemResponse.HasEnoughStock), o => o.MapFrom(i => i.Variant!.Stock >= i.Quantity));

        CreateMap<OrderItem, OrderItemResponse>()
            .ForCtorParam(nameof(OrderItemResponse.LineTotalEgp), o => o.MapFrom(i => i.UnitPriceEgp * i.Quantity));

        CreateMap<Order, OrderResponse>()
            .ForCtorParam(nameof(OrderResponse.RewardPending), o => o.MapFrom(order =>
                order.TotalRewardPoints > 0 && order.Status != OrderStatus.Delivered && order.Status != OrderStatus.Cancelled))
            .ForCtorParam(nameof(OrderResponse.CanCancel), o => o.MapFrom(order => OrderStatusFlow.MemberCanCancel(order.Status)))
            .ForCtorParam(nameof(OrderResponse.NextStatuses), o => o.MapFrom(order => OrderStatusFlow.NextStatuses(order.Status)))
            .ForCtorParam(nameof(OrderResponse.Items), o => o.MapFrom(order => order.Items.OrderBy(i => i.ProductName)))
            .ForCtorParam(nameof(OrderResponse.StatusHistory), o => o.MapFrom(order => order.StatusHistory.OrderBy(h => h.ChangedAt)));

        CreateMap<Order, OrderSummaryResponse>()
            .ForCtorParam(nameof(OrderSummaryResponse.ItemCount), o => o.MapFrom(order => order.Items.Sum(i => i.Quantity)))
            .ForCtorParam(nameof(OrderSummaryResponse.RecipientName), o => o.MapFrom(order => order.ShippingAddress.FullName))
            .ForCtorParam(nameof(OrderSummaryResponse.City), o => o.MapFrom(order => order.ShippingAddress.City));
    }

    private void MapSubmissions()
    {
        CreateMap<SubmissionCategory, SubmissionCategoryResponse>();

        // Built by hand: the fields come from the submission, its category and the member's name.
        CreateMap<Submissions.SubmissionView, SubmissionResponse>().ConvertUsing(v => new SubmissionResponse(
            v.Submission.Id,
            v.Submission.UserId,
            v.MemberName,
            v.Submission.CategoryId,
            v.Submission.Category!.Name,
            v.Submission.Category.PointsReward,
            v.Submission.PhotoUrl,
            v.Submission.Note,
            v.Submission.WeightKg,
            v.Submission.Status,
            v.Submission.ReviewedBy,
            v.Submission.ReviewedAt,
            v.Submission.RejectReason,
            v.Submission.CreatedAt));
    }

    /// <summary>The first image in display order, or null.</summary>
    private static string? MainImageUrl(Product? product) =>
        product?.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault();

    /// <summary>Stored JSON text → a JSON value in the response (not an escaped string).</summary>
    private static JsonElement? ParseJson(string? json)
    {
        if (json is null)
        {
            return null;
        }

        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}

/// <summary>The data <see cref="MeResponse"/> is built from: the login account, the profile and the roles.</summary>
internal sealed record MeSource(UserAccount Account, UserProfile Profile, IReadOnlyList<string> Roles);
