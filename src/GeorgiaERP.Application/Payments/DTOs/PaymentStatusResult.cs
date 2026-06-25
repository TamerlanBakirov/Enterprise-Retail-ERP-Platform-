using GeorgiaERP.Domain.Payments;

namespace GeorgiaERP.Application.Payments.DTOs;

public record PaymentStatusResult(
    PaymentStatus Status,
    string? ExternalTransactionId,
    string? ErrorMessage);
