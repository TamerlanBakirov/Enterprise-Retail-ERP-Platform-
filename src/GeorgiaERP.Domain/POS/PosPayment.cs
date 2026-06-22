using GeorgiaERP.Domain.Common;
using GeorgiaERP.Domain.POS.Events;

namespace GeorgiaERP.Domain.POS;

public enum PaymentMethod
{
    Cash,
    Card,
    BankTransfer,
    Loyalty,
    Mixed
}

public class PosPayment : BaseEntity
{
    public Guid TransactionId { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "GEL";
    public string? Reference { get; private set; }
    public string? TerminalRef { get; private set; }
    public decimal? ChangeAmount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // Navigation properties
    public PosTransaction Transaction { get; private set; } = default!;

    private PosPayment() { }

    public static PosPayment Create(Guid transactionId, PaymentMethod paymentMethod, decimal amount, string currency = "GEL")
    {
        var payment = new PosPayment
        {
            TransactionId = transactionId,
            PaymentMethod = paymentMethod,
            Amount = amount,
            Currency = currency,
            CreatedAt = DateTimeOffset.UtcNow
        };

        payment.RaiseDomainEvent(new PaymentProcessedEvent
        {
            PaymentId = payment.Id,
            TransactionId = transactionId,
            PaymentMethod = paymentMethod,
            Amount = amount,
            Currency = currency
        });

        return payment;
    }

    public void SetReference(string? reference, string? terminalRef = null)
    {
        Reference = reference;
        TerminalRef = terminalRef;
    }

    public void SetChange(decimal changeAmount) => ChangeAmount = changeAmount;
}
