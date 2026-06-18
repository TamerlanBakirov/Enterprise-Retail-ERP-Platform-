using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.POS;

public class PosTransactionLine : BaseEntity
{
    public Guid TransactionId { get; private set; }
    public int LineNumber { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? VariantId { get; private set; }
    public string? Barcode { get; private set; }
    public string ProductName { get; private set; } = default!;
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public string? DiscountReason { get; private set; }
    public decimal VatAmount { get; private set; }
    public decimal LineTotal { get; private set; }
    public decimal CostPrice { get; private set; }
    public Guid? PromotionId { get; private set; }

    // Navigation properties
    public PosTransaction Transaction { get; private set; } = default!;

    private PosTransactionLine() { }

    public static PosTransactionLine Create(
        Guid transactionId,
        int lineNumber,
        Guid productId,
        string productName,
        decimal quantity,
        decimal unitPrice)
    {
        return new PosTransactionLine
        {
            TransactionId = transactionId,
            LineNumber = lineNumber,
            ProductId = productId,
            ProductName = productName,
            Quantity = quantity,
            UnitPrice = unitPrice
        };
    }
}
