using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace RowCycle.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "Postgres";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        GetConnectionString(configuration);
        return services;
    }

    public static string GetConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is missing. Set it with " +
                $"'dotnet user-secrets set \"ConnectionStrings:{ConnectionStringName}\" \"<value>\" --project backend/src/RowCycle.Api' " +
                $"or the ConnectionStrings__{ConnectionStringName} environment variable.");
        }

        return connectionString;
    }
}
