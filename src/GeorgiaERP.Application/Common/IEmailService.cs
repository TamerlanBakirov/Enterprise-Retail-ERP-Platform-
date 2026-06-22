namespace GeorgiaERP.Application.Common;

/// <summary>
/// Abstraction for sending email notifications. Implementations may use SMTP,
/// SendGrid, or any other email delivery provider.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a single email message.
    /// </summary>
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a batch of email messages. Implementations should handle partial
    /// failures gracefully and log individual send errors.
    /// </summary>
    Task SendBatchAsync(IReadOnlyList<EmailMessage> messages, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a single email message to be sent.
/// </summary>
public sealed record EmailMessage
{
    public required string To { get; init; }
    public required string Subject { get; init; }
    public required string HtmlBody { get; init; }
    public string? PlainTextBody { get; init; }
    public string? ReplyTo { get; init; }
    public EmailPriority Priority { get; init; } = EmailPriority.Normal;

    /// <summary>
    /// Optional tag for tracking/categorization (e.g., "low-stock-alert", "waybill-failure").
    /// </summary>
    public string? Tag { get; init; }
}

/// <summary>
/// Email priority levels.
/// </summary>
public enum EmailPriority
{
    Low,
    Normal,
    High
}
