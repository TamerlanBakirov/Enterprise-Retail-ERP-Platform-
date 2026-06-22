using System.Net;
using System.Net.Mail;
using GeorgiaERP.Application.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GeorgiaERP.Infrastructure.Email;

/// <summary>
/// SMTP-based email service implementation. Uses System.Net.Mail for broad
/// compatibility with SMTP providers (Gmail, Outlook, SendGrid SMTP relay, etc.).
/// </summary>
public sealed class SmtpEmailService : IEmailService, IDisposable
{
    private readonly SmtpEmailOptions _options;
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly SmtpClient _client;

    public SmtpEmailService(IOptions<SmtpEmailOptions> options, ILogger<SmtpEmailService> logger)
    {
        _options = options.Value;
        _logger = logger;

        _client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = _options.TimeoutMs
        };

        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            _client.Credentials = new NetworkCredential(_options.Username, _options.Password);
        }
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("Email sending disabled. Would have sent '{Subject}' to {To}",
                message.Subject, message.To);
            return;
        }

        using var mailMessage = BuildMailMessage(message);

        try
        {
            await _client.SendMailAsync(mailMessage, cancellationToken);
            _logger.LogInformation(
                "Email sent successfully: '{Subject}' to {To} [tag={Tag}]",
                message.Subject, message.To, message.Tag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send email: '{Subject}' to {To} [tag={Tag}]",
                message.Subject, message.To, message.Tag);
            throw;
        }
    }

    public async Task SendBatchAsync(IReadOnlyList<EmailMessage> messages, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("Email sending disabled. Would have sent {Count} messages", messages.Count);
            return;
        }

        var sent = 0;
        var failed = 0;

        foreach (var message in messages)
        {
            try
            {
                await SendAsync(message, cancellationToken);
                sent++;
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogWarning(ex, "Batch send: failed to send email to {To}", message.To);
            }
        }

        _logger.LogInformation("Batch email send completed: {Sent} sent, {Failed} failed out of {Total}",
            sent, failed, messages.Count);
    }

    private MailMessage BuildMailMessage(EmailMessage message)
    {
        var from = new MailAddress(_options.FromAddress, _options.FromName);
        var mail = new MailMessage(from, new MailAddress(message.To))
        {
            Subject = message.Subject,
            Body = message.HtmlBody,
            IsBodyHtml = true
        };

        if (!string.IsNullOrWhiteSpace(message.PlainTextBody))
        {
            var plainView = AlternateView.CreateAlternateViewFromString(
                message.PlainTextBody, null, "text/plain");
            var htmlView = AlternateView.CreateAlternateViewFromString(
                message.HtmlBody, null, "text/html");
            mail.AlternateViews.Add(plainView);
            mail.AlternateViews.Add(htmlView);
        }

        if (!string.IsNullOrWhiteSpace(message.ReplyTo))
        {
            mail.ReplyToList.Add(new MailAddress(message.ReplyTo));
        }

        mail.Priority = message.Priority switch
        {
            EmailPriority.High => MailPriority.High,
            EmailPriority.Low => MailPriority.Low,
            _ => MailPriority.Normal
        };

        return mail;
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}

/// <summary>
/// Configuration options for SMTP email delivery.
/// Bound from appsettings "Email:Smtp" section.
/// </summary>
public sealed class SmtpEmailOptions
{
    public const string SectionName = "Email:Smtp";

    /// <summary>Whether email sending is enabled. Defaults to false so
    /// development environments don't accidentally send emails.</summary>
    public bool Enabled { get; set; }

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = "noreply@georgiaerp.ge";
    public string FromName { get; set; } = "Georgia ERP Platform";
    public int TimeoutMs { get; set; } = 30000;
}
