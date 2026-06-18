using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Compliance;

public enum WaybillStatus
{
    Draft,
    Saved,
    Active,
    Confirmed,
    Rejected,
    Closed,
    Expired
}

public class RsGeWaybill : BaseEntity
{
    public Guid FiscalDocumentId { get; private set; }
    public string? WaybillNumber { get; private set; }
    public string? WaybillType { get; private set; }
    public string? SellerTin { get; private set; }
    public string? SellerName { get; private set; }
    public string? BuyerTin { get; private set; }
    public string? BuyerName { get; private set; }
    public string? TransporterTin { get; private set; }
    public string? TransportType { get; private set; }
    public string? VehicleNumber { get; private set; }
    public string? DriverTin { get; private set; }
    public string? StartAddress { get; private set; }
    public string? EndAddress { get; private set; }
    public string? GoodsData { get; private set; } // jsonb
    public decimal? TotalAmount { get; private set; }
    public DateTimeOffset? ActivateDate { get; private set; }
    public DateTimeOffset? DeliveryDate { get; private set; }
    public WaybillStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Navigation properties
    public FiscalDocument FiscalDocument { get; private set; } = default!;

    private RsGeWaybill() { }

    public static RsGeWaybill Create(Guid fiscalDocumentId, string? waybillType = null)
    {
        return new RsGeWaybill
        {
            FiscalDocumentId = fiscalDocumentId,
            WaybillType = waybillType,
            Status = WaybillStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
