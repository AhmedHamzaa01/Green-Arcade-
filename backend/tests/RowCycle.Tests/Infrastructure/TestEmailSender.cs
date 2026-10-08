using System.Collections.Concurrent;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;
using RowCycle.Application.Common;

namespace RowCycle.Tests.Infrastructure;

/// <summary>Keeps sent emails in memory so tests can follow verification and reset links.</summary>
public sealed partial class TestEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<SentEmail> _sent = new();

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        _sent.Enqueue(new SentEmail(to, subject, htmlBody));
        return Task.CompletedTask;
    }

    public IReadOnlyList<SentEmail> SentTo(string email) =>
        _sent.Where(e => string.Equals(e.To, email, StringComparison.OrdinalIgnoreCase)).ToList();

    /// <summary>Query string values of the newest link to <paramref name="path"/> sent to <paramref name="email"/>.</summary>
    public Dictionary<string, string> LastLinkQuery(string email, string path)
    {
        var body = SentTo(email).Last(e => e.HtmlBody.Contains($"/{path}?")).HtmlBody;
        var href = WebUtility.HtmlDecode(HrefRegex().Match(body).Groups[1].Value);
        var query = QueryHelpers.ParseQuery(new Uri(href).Query);
        return query.ToDictionary(q => q.Key, q => q.Value.ToString());
    }

    [GeneratedRegex("href=\"([^\"]+)\"")]
    private static partial Regex HrefRegex();
}

public sealed record SentEmail(string To, string Subject, string HtmlBody);
