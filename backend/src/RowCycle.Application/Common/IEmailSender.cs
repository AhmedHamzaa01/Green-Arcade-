namespace RowCycle.Application.Common;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
