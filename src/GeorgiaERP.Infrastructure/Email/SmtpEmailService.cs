using System.Net;
using System.Net.Mail;
using GeorgiaERP.Application.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GeorgiaERP.Infrastructure.Email;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var smtpHost = _configuration["Email:SmtpHost"];
        if (string.IsNullOrWhiteSpace(smtpHost))
        {
            _logger.LogWarning("Email:SmtpHost is not configured. Skipping email send to {To} with subject '{Subject}'",
                message.To, message.Subject);
            return;
        }

        var port = int.TryParse(_configuration["Email:SmtpPort"], out var p) ? p : 587;
        var username = _configuration["Email:Username"];
        var password = _configuration["Email:Password"];
        var fromAddress = message.From ?? _configuration["Email:FromAddress"] ?? "noreply@georgia-erp.local";
        var fromName = _configuration["Email:FromName"] ?? "Georgia ERP";
        var useSsl = !bool.TryParse(_configuration["Email:UseSsl"], out var ssl) || ssl;

        try
        {
            using var mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(fromAddress, fromName);
            mailMessage.To.Add(message.To);
            mailMessage.Subject = message.Subject;
            mailMessage.IsBodyHtml = true;
            mailMessage.Body = message.HtmlBody;

            if (!string.IsNullOrWhiteSpace(message.PlainTextBody))
            {
                var plainView = AlternateView.CreateAlternateViewFromString(message.PlainTextBody, null, "text/plain");
                var htmlView = AlternateView.CreateAlternateViewFromString(message.HtmlBody, null, "text/html");
                mailMessage.AlternateViews.Add(plainView);
                mailMessage.AlternateViews.Add(htmlView);
            }

            using var client = new SmtpClient(smtpHost, port);
            client.EnableSsl = useSsl;

            if (!string.IsNullOrWhiteSpace(username))
                client.Credentials = new NetworkCredential(username, password);

            await client.SendMailAsync(mailMessage, cancellationToken);
            _logger.LogInformation("Email sent successfully to {To} with subject '{Subject}'", message.To, message.Subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} with subject '{Subject}'", message.To, message.Subject);
            throw;
        }
    }
}
