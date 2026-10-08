using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RowCycle.Infrastructure.Auth;

/// <summary>
/// Email verification links last 24 hours; the default provider (used for password reset) is set to 1 hour (FR-04).
/// </summary>
internal sealed class EmailConfirmationTokenProvider<TUser>(
    IDataProtectionProvider dataProtectionProvider,
    IOptions<EmailConfirmationTokenProviderOptions> options,
    ILogger<DataProtectorTokenProvider<TUser>> logger)
    : DataProtectorTokenProvider<TUser>(dataProtectionProvider, options, logger)
    where TUser : class
{
    public const string ProviderName = "EmailConfirmation";
}

internal sealed class EmailConfirmationTokenProviderOptions : DataProtectionTokenProviderOptions
{
    public EmailConfirmationTokenProviderOptions()
    {
        Name = "EmailConfirmationDataProtectorTokenProvider";
        TokenLifespan = TimeSpan.FromHours(24);
    }
}
