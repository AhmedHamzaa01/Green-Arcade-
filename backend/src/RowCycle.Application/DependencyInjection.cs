using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using RowCycle.Application.Auth;
using RowCycle.Application.Points;

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
        return services;
    }
}
