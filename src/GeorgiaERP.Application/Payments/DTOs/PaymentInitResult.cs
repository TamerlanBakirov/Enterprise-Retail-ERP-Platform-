namespace GeorgiaERP.Application.Payments.DTOs;

public record PaymentInitResult(
    bool IsSuccess,
    string? ExternalTransactionId,
    string? RedirectUrl,
    string? ErrorMessage)
{
    public static PaymentInitResult Success(string externalId, string redirectUrl) =>
        new(true, externalId, redirectUrl, null);
    public static PaymentInitResult Failure(string error) =>
        new(false, null, null, error);
}
