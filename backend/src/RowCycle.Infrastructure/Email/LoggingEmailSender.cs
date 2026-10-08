using Microsoft.Extensions.Logging;
using RowCycle.Application.Common;

namespace RowCycle.Infrastructure.Email;

/// <summary>Development sender: writes the email to the log instead of sending it. Copy links from the console.</summary>
internal sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Email to {To}. Subject: {Subject}\n{Body}", to, subject, htmlBody);
        return Task.CompletedTask;
    }
}
