namespace GeorgiaERP.Application.Payments.DTOs;

public record PaymentTransactionDto(
    Guid Id,
    Guid OrderId,
    decimal Amount,
    string Currency,
    string Provider,
    string Status,
    string? ExternalTransactionId,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    string? Metadata);
