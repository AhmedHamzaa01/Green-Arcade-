using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using RowCycle.Application.Addresses;
using RowCycle.Application.Audit;
using RowCycle.Application.Auth;
using RowCycle.Application.Cart;
using RowCycle.Application.Catalog;
using RowCycle.Application.Orders;
using RowCycle.Application.Points;
using RowCycle.Application.Settings;

namespace RowCycle.Application;

public static class DependencyInjection
{
    /// <param name="autoMapperLicenseKey">From <c>AutoMapper:LicenseKey</c>. Without it AutoMapper still works but logs a licence warning.</param>
    public static IServiceCollection AddApplication(this IServiceCollection services, string? autoMapperLicenseKey = null)
    {
        services.AddAutoMapper(cfg => cfg.LicenseKey = autoMapperLicenseKey, typeof(DependencyInjection).Assembly);
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPointsService, PointsService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<ICatalogAdminService, CatalogAdminService>();
        services.AddScoped<IAddressService, AddressService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<OrderWorkflow>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IOrderAdminService, OrderAdminService>();
        return services;
    }
}
