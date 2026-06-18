using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.POS;

public enum PosTransactionType
{
    Sale,
    Return,
    Exchange,
    Void
}

public enum PosTransactionStatus
{
    Completed,
    Voided,
    Pending
}

public class PosTransaction : BaseEntity
{
    public string TransactionNumber { get; private set; } = default!;
    public Guid SessionId { get; private set; }
    public Guid StoreId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public PosTransactionType TransactionType { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal DiscountTotal { get; private set; }
    public decimal VatTotal { get; private set; }
    public decimal Total { get; private set; }
    public PosTransactionStatus Status { get; private set; }
    public string? FiscalReceiptId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset? VoidedAt { get; private set; }
    public Guid? VoidedBy { get; private set; }
    public string? VoidReason { get; private set; }

    // Navigation properties
    public PosSession Session { get; private set; } = default!;
    public ICollection<PosTransactionLine> Lines { get; private set; } = new List<PosTransactionLine>();
    public ICollection<PosPayment> Payments { get; private set; } = new List<PosPayment>();

    private PosTransaction() { }

    public static PosTransaction Create(
        string transactionNumber,
        Guid sessionId,
        Guid storeId,
        PosTransactionType transactionType,
        Guid createdBy)
    {
        return new PosTransaction
        {
            TransactionNumber = transactionNumber,
            SessionId = sessionId,
            StoreId = storeId,
            TransactionType = transactionType,
            Status = PosTransactionStatus.Pending,
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
