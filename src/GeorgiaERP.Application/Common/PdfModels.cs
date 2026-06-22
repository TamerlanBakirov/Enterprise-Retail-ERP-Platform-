namespace GeorgiaERP.Application.Common;

public record ReceiptData
{
    public string TransactionNumber { get; init; } = default!;
    public DateTimeOffset Date { get; init; }
    public string StoreName { get; init; } = default!;
    public string? StoreAddress { get; init; }
    public string? CompanyName { get; init; }
    public string? CompanyTin { get; init; }
    public string? CashierName { get; init; }
    public string? TerminalId { get; init; }
    public List<ReceiptLineData> Lines { get; init; } = [];
    public decimal Subtotal { get; init; }
    public decimal DiscountTotal { get; init; }
    public decimal VatTotal { get; init; }
    public decimal Total { get; init; }
    public List<ReceiptPaymentData> Payments { get; init; } = [];
    public string? FiscalReceiptId { get; init; }
    public string? CustomerName { get; init; }
    public int? LoyaltyPointsEarned { get; init; }
}

public record ReceiptLineData(string ProductName, decimal Quantity, string Unit, decimal UnitPrice, decimal Discount, decimal Total);

public record ReceiptPaymentData(string Method, decimal Amount, decimal? Change);

public record InvoiceData
{
    public string InvoiceNumber { get; init; } = default!;
    public DateTimeOffset Date { get; init; }
    public DateTimeOffset? DueDate { get; init; }
    public string SellerName { get; init; } = default!;
    public string? SellerNameKa { get; init; }
    public string SellerTin { get; init; } = default!;
    public string? SellerAddress { get; init; }
    public string? SellerPhone { get; init; }
    public string? SellerEmail { get; init; }
    public bool SellerIsVatPayer { get; init; }
    public string? BuyerName { get; init; }
    public string? BuyerTin { get; init; }
    public string? BuyerAddress { get; init; }
    public List<InvoiceLineData> Lines { get; init; } = [];
    public decimal Subtotal { get; init; }
    public decimal VatTotal { get; init; }
    public decimal Total { get; init; }
    public string? Notes { get; init; }
    public string? BankName { get; init; }
    public string? BankAccount { get; init; }
    public string Currency { get; init; } = "GEL";
}

public record InvoiceLineData(int LineNumber, string ProductName, string Unit, decimal Quantity, decimal UnitPrice, decimal VatAmount, decimal Total);
