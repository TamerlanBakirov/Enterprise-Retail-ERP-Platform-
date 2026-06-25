using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Payments;

public enum PaymentProvider
{
    BankOfGeorgia,
    TbcBank,
    Cash,
    BankTransfer
}

public enum PaymentStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Refunded,
    Cancelled
}

public class PaymentTransaction : BaseEntity
{
    public Guid OrderId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "GEL";
    public PaymentProvider Provider { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string? ExternalTransactionId { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? Metadata { get; private set; }

    private PaymentTransaction() { }

    public static PaymentTransaction Create(
        Guid orderId,
        decimal amount,
        PaymentProvider provider,
        string currency = "GEL",
        string? metadata = null)
    {
        if (amount <= 0)
            throw new ArgumentException("Payment amount must be greater than zero.", nameof(amount));

        return new PaymentTransaction
        {
            OrderId = orderId,
            Amount = amount,
            Currency = currency,
            Provider = provider,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            Metadata = metadata
        };
    }

    public void MarkProcessing(string externalTransactionId)
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"Cannot mark payment as processing from status '{Status}'.");

        Status = PaymentStatus.Processing;
        ExternalTransactionId = externalTransactionId;
    }

    public void MarkCompleted(string? externalTransactionId = null)
    {
        if (Status != PaymentStatus.Pending && Status != PaymentStatus.Processing)
            throw new InvalidOperationException($"Cannot complete payment from status '{Status}'.");

        Status = PaymentStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
        if (externalTransactionId is not null)
            ExternalTransactionId = externalTransactionId;
    }

    public void MarkFailed(string errorMessage)
    {
        if (Status == PaymentStatus.Completed || Status == PaymentStatus.Refunded)
            throw new InvalidOperationException($"Cannot fail payment from status '{Status}'.");

        Status = PaymentStatus.Failed;
        ErrorMessage = errorMessage;
    }

    public void MarkRefunded()
    {
        if (Status != PaymentStatus.Completed)
            throw new InvalidOperationException($"Cannot refund payment from status '{Status}'.");

        Status = PaymentStatus.Refunded;
    }

    public void MarkCancelled()
    {
        if (Status != PaymentStatus.Pending && Status != PaymentStatus.Processing)
            throw new InvalidOperationException($"Cannot cancel payment from status '{Status}'.");

        Status = PaymentStatus.Cancelled;
    }
}
