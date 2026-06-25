using GeorgiaERP.Application.Payments.DTOs;
using GeorgiaERP.Domain.Payments;

namespace GeorgiaERP.Application.Payments;

public interface IPaymentGateway
{
    PaymentProvider Provider { get; }
    Task<PaymentInitResult> InitiatePaymentAsync(decimal amount, string currency, Guid orderId, string returnUrl);
    Task<PaymentStatusResult> CheckStatusAsync(string externalTransactionId);
    Task<RefundResult> RefundAsync(string externalTransactionId, decimal amount);
}
