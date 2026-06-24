namespace GeorgiaERP.Application.Compliance;

/// <summary>
/// Application-layer abstraction for the RS.GE Revenue Service SOAP client.
/// Infrastructure provides the concrete HTTP/SOAP implementation.
/// </summary>
public interface IRsGeSoapClient
{
    Task<string> GetMyIpAsync();
    Task<RsGeServiceUser> CheckServiceUserAsync(string serviceUser, string servicePassword);
    Task<RsGeNameResult> GetNameFromTinAsync(string tin);
    Task<bool> IsVatPayerAsync(string tin);
    Task<IReadOnlyList<RsGeUnit>> GetUnitsAsync();
    Task<IReadOnlyList<RsGeTransportType>> GetTransportTypesAsync();
    Task<IReadOnlyList<RsGeWaybillType>> GetWaybillTypesAsync();
    Task<RsGeWaybillResult> SaveWaybillAsync(RsGeWaybillRequest request);
    Task<RsGeResult> SendWaybillAsync(int waybillId);
    Task<RsGeResult> ConfirmWaybillAsync(int waybillId);
    Task<RsGeResult> CloseWaybillAsync(int waybillId);
    Task<RsGeResult> RejectWaybillAsync(int waybillId);
    Task<RsGeWaybillData?> GetWaybillAsync(int waybillId);
    Task<RsGeResult> SaveInvoiceAsync(RsGeInvoiceRequest request);
    Task<RsGeResult> SubmitVatDeclarationAsync(RsGeVatDeclarationRequest request);
}

// DTOs shared between Application and Infrastructure layers
public record RsGeServiceUser(string PayerId, string UserId);
public record RsGeNameResult(string Name, bool Found);
public record RsGeUnit(int Id, string Name);
public record RsGeTransportType(int Id, string Name);
public record RsGeWaybillType(int Id, string Name);
public record RsGeResult(bool Success, string? ErrorCode, string? ErrorMessage);
public record RsGeWaybillResult(bool Success, int? WaybillId, string? WaybillNumber, string? ErrorCode, string? ErrorMessage);

public record RsGeWaybillRequest
{
    public int WaybillType { get; init; }
    public string BuyerTin { get; init; } = "";
    public string StartAddress { get; init; } = "";
    public string EndAddress { get; init; } = "";
    public int? TransportTypeId { get; init; }
    public string? CarNumber { get; init; }
    public string? DriverTin { get; init; }
    public string? Comment { get; init; }
    public List<RsGeGoodsItem> Goods { get; init; } = new();
}

public record RsGeGoodsItem(string ProductName, int UnitId, decimal Quantity, decimal Price, string? BarCode);

public record RsGeInvoiceRequest
{
    public string BuyerTin { get; init; } = "";
    public string BuyerName { get; init; } = "";
    public DateTimeOffset InvoiceDate { get; init; }
    public List<RsGeInvoiceItem> Items { get; init; } = new();
}

public record RsGeInvoiceItem(string Description, decimal Quantity, decimal UnitPrice, decimal VatAmount);

public record RsGeVatDeclarationRequest
{
    public DateTimeOffset PeriodStart { get; init; }
    public DateTimeOffset PeriodEnd { get; init; }
    public decimal TotalOutputVat { get; init; }
    public decimal TotalInputVat { get; init; }
    public decimal NetVat { get; init; }
}

public record RsGeWaybillData
{
    public int Id { get; init; }
    public string? WaybillNumber { get; init; }
    public int WaybillType { get; init; }
    public string? SellerTin { get; init; }
    public string? BuyerTin { get; init; }
    public int Status { get; init; }
    public string? StatusText { get; init; }
}
