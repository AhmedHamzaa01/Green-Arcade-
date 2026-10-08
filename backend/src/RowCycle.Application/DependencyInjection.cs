using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using RowCycle.Application.Auth;

namespace RowCycle.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
