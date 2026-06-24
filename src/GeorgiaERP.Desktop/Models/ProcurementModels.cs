namespace GeorgiaERP.Desktop.Models;

public record SupplierDto(
    Guid Id,
    string Code,
    string Name,
    string? NameKa,
    string? TaxId,
    string? Phone,
    string? Email,
    string? Address,
    int? PaymentTermDays,
    bool IsVatPayer,
    bool IsActive,
    DateTimeOffset CreatedAt);

public record PurchaseOrderDto(
    Guid Id,
    string PoNumber,
    Guid SupplierId,
    string? SupplierName,
    string Status,
    decimal Subtotal,
    decimal VatTotal,
    decimal Total,
    string? Notes,
    DateTimeOffset CreatedAt);

public record CreateSupplierRequest(
    string Code,
    string Name,
    string? NameKa,
    string? TaxId,
    string? Phone,
    string? Email,
    string? Address,
    int? PaymentTermDays,
    bool IsVatPayer);

public record CreatePurchaseOrderRequest(
    Guid SupplierId,
    Guid WarehouseId,
    List<PurchaseOrderLineRequest> Lines,
    string? Notes);

public record PurchaseOrderLineRequest(
    Guid ProductId,
    decimal Quantity,
    decimal UnitPrice);

public record ReceiveGoodsRequest(
    Guid PurchaseOrderId,
    List<ReceiveGoodsLineRequest> Lines,
    string? Notes);

public record ReceiveGoodsLineRequest(
    Guid PurchaseOrderLineId,
    decimal ReceivedQuantity,
    string? QualityResult,
    string? BatchNumber);
