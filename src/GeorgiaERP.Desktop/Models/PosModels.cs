namespace GeorgiaERP.Desktop.Models;

public record PosSessionDto(
    Guid Id,
    Guid TerminalId,
    string TerminalName,
    Guid CashierId,
    string CashierName,
    string Status,
    decimal OpeningBalance,
    decimal? ClosingBalance,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt);

public record PosTransactionDto(
    Guid Id,
    string TransactionNumber,
    Guid SessionId,
    Guid StoreId,
    string StoreName,
    string Status,
    decimal SubTotal,
    decimal TotalVat,
    decimal TotalDiscount,
    decimal GrandTotal,
    Guid? CustomerId,
    string? CustomerName,
    DateTimeOffset CreatedAt);

public record PosTransactionDetailDto(
    Guid Id,
    string TransactionNumber,
    string Status,
    decimal SubTotal,
    decimal TotalVat,
    decimal TotalDiscount,
    decimal GrandTotal,
    List<PosLineDto> Lines,
    List<PosPaymentDto> Payments,
    DateTimeOffset CreatedAt);

public record PosLineDto(
    Guid ProductId,
    string ProductName,
    string? Barcode,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountAmount,
    decimal VatAmount,
    decimal LineTotal);

public record PosPaymentDto(
    string PaymentMethod,
    decimal Amount,
    decimal? ChangeAmount,
    string? Reference);

public record CreatePosTransactionRequest(
    Guid SessionId,
    Guid? CustomerId,
    List<PosLineRequest> Lines,
    List<PosPaymentRequest> Payments);

public record PosLineRequest(
    Guid? ProductId,
    string? Barcode,
    decimal Quantity,
    decimal? DiscountAmount);

public record PosPaymentRequest(
    string PaymentMethod,
    decimal Amount);

public record TerminalDto(
    Guid Id,
    string Code,
    string Name,
    Guid StoreId,
    string TerminalType,
    bool IsActive,
    DateTimeOffset CreatedAt);

public record OpenPosSessionRequest(
    Guid TerminalId,
    decimal OpeningBalance);

public record ClosePosSessionRequest(
    decimal ClosingBalance);
