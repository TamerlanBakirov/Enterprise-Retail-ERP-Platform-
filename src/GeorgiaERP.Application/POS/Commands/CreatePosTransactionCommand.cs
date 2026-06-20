using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.POS;
using MediatR;

namespace GeorgiaERP.Application.POS.Commands;

public record CreatePosTransactionCommand(
    Guid SessionId,
    Guid? CustomerId,
    List<PosLineInput> Lines,
    List<PosPaymentInput> Payments) : IRequest<Result<PosTransactionResponse>>;

public record PosLineInput(
    Guid? ProductId,
    string? Barcode,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountAmount = 0,
    string? DiscountReason = null);

public record PosPaymentInput(
    PaymentMethod PaymentMethod,
    decimal Amount,
    string? Reference = null,
    string? TerminalRef = null);

public record PosTransactionResponse(
    Guid TransactionId,
    string TransactionNumber,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal VatTotal,
    decimal Total,
    Guid? FiscalDocumentId,
    string Status);
