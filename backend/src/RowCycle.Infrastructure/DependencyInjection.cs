using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RowCycle.Application.Audit;
using RowCycle.Application.Auth;
using RowCycle.Application.Common;
using RowCycle.Application.Points;
using RowCycle.Application.Settings;
using RowCycle.Application.Users;
using RowCycle.Infrastructure.Auth;
using RowCycle.Infrastructure.Email;
using RowCycle.Infrastructure.Identity;
using RowCycle.Infrastructure.Persistence;
using RowCycle.Infrastructure.Persistence.Repositories;

namespace RowCycle.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "Postgres";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = GetConnectionString(configuration);

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<SoftDeleteInterceptor>();
        services.AddDbContext<AppDbContext>((sp, options) => options
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(sp.GetRequiredService<SoftDeleteInterceptor>())
            // Expected with soft delete: rows linked to a soft-deleted badge/category/variant are hidden by default.
            // History views that must still show them use IgnoreQueryFilters().
            .ConfigureWarnings(w => w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning)));

        AddIdentity(services);

        services.AddOptions<JwtOptions>().Bind(configuration.GetSection(JwtOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<AppOptions>().Bind(configuration.GetSection(AppOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<AdminSeedOptions>().Bind(configuration.GetSection(AdminSeedOptions.SectionName));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IPointsLedgerRepository, PointsLedgerRepository>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();
        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();
        services.AddSingleton<IEmailSender, LoggingEmailSender>();

        return services;
    }

    private static void AddIdentity(IServiceCollection services)
    {
        services.AddDataProtection();
        services.AddIdentityCore<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = true;

                // FR-01: min 8 chars with a digit; the "contains a letter" rule is in the FluentValidation validator.
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;

                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

                options.Tokens.EmailConfirmationTokenProvider = EmailConfirmationTokenProvider<AppUser>.ProviderName;
            })
            .AddRoles<AppRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders()
            .AddTokenProvider<EmailConfirmationTokenProvider<AppUser>>(EmailConfirmationTokenProvider<AppUser>.ProviderName);

        // Password reset tokens (default provider) are valid for 1 hour (FR-04).
        services.Configure<DataProtectionTokenProviderOptions>(o => o.TokenLifespan = TimeSpan.FromHours(1));
    }

    /// <summary>Applies pending migrations. Called at startup when <c>Database:MigrateOnStartup</c> is true (Development, tests).</summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }

    /// <summary>Creates the admin user from the <c>Admin</c> config section, if set.</summary>
    public static Task SeedAdminAsync(this IServiceProvider services, CancellationToken cancellationToken = default) =>
        AdminSeeder.SeedAsync(services, cancellationToken);

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
