namespace GeorgiaERP.Desktop.Models;

public record RsGeHealthDto(string Service, string Status, DateTimeOffset Timestamp);

public record WaybillDto(
    Guid Id,
    Guid FiscalDocumentId,
    string? WaybillNumber,
    string WaybillType,
    string SellerTin,
    string BuyerTin,
    string? BuyerName,
    string Status,
    decimal TotalAmount,
    string? StartAddress,
    string? EndAddress,
    DateTimeOffset CreatedAt);

public record FiscalDocumentDto(
    Guid Id,
    string DocumentType,
    string? DocumentNumber,
    string? InternalRef,
    string Status,
    string? RsGeId,
    string? RsGeStatus,
    DateTimeOffset? SubmissionDeadline,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? ConfirmedAt,
    int RetryCount,
    string? LastError,
    DateTimeOffset CreatedAt);

public record VatSummaryDto(
    string Period,
    decimal OutputVat,
    decimal InputVat,
    decimal NetVat,
    string Status,
    string Currency);

public record DeadlinesResponse(
    DateTimeOffset CheckedAt,
    int OverdueCount,
    int DueSoonCount,
    List<DeadlineDocumentDto> Documents);

public record DeadlineDocumentDto(
    Guid Id,
    string Type,
    string? InternalRef,
    string Status,
    DateTimeOffset? Deadline,
    bool IsOverdue,
    string? LastError);

public record TinLookupResult(string? Name);
public record VatStatusResult(string Tin, bool IsVatPayer);

public record CreateWaybillRequest(
    string WaybillType,
    string BuyerTin,
    string? BuyerName,
    string SellerTin,
    string? SellerName,
    string? StartAddress,
    string? EndAddress,
    string? VehicleNumber,
    string? DriverTin,
    string? TransportType,
    string? InternalRef,
    Guid? ReferenceId,
    string? ReferenceType,
    List<WaybillGoodsItem>? Goods);

public record WaybillGoodsItem(
    string ProductName,
    int UnitId,
    decimal Quantity,
    decimal Price,
    string? BarCode);
