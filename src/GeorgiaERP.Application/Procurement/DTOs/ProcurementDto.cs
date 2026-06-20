namespace GeorgiaERP.Application.Procurement.DTOs;

public record SupplierDto(
    Guid Id,
    string Code,
    string Name,
    string? NameKa,
    string? Tin,
    bool IsVatPayer,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? Address,
    string? PaymentTerms,
    decimal? CreditLimit,
    int? Rating,
    bool IsActive,
    DateTimeOffset CreatedAt);

public record PurchaseOrderDto(
    Guid Id,
    string PoNumber,
    Guid SupplierId,
    string? SupplierName,
    Guid WarehouseId,
    string Status,
    DateTimeOffset OrderDate,
    DateTimeOffset? ExpectedDate,
    decimal Subtotal,
    decimal VatTotal,
    decimal Total,
    string? Notes,
    Guid CreatedBy,
    DateTimeOffset CreatedAt,
    IReadOnlyList<PurchaseOrderLineDto> Lines);

public record PurchaseOrderLineDto(
    Guid Id,
    int LineNumber,
    Guid ProductId,
    string? ProductName,
    decimal OrderedQty,
    decimal ReceivedQty,
    decimal UnitPrice,
    decimal VatAmount,
    decimal LineTotal);

public record CreateSupplierRequest(
    string Code,
    string Name,
    string? NameKa,
    string? Tin,
    bool IsVatPayer,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? Address,
    string? PaymentTerms,
    decimal? CreditLimit);
