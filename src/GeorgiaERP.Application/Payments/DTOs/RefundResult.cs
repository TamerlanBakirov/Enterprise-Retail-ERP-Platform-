namespace GeorgiaERP.Application.Payments.DTOs;

public record RefundResult(
    bool IsSuccess,
    string? ErrorMessage)
{
    public static RefundResult Success() => new(true, null);
    public static RefundResult Failure(string error) => new(false, error);
}
