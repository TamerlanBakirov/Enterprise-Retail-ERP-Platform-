using GeorgiaERP.Domain.Common;
using GeorgiaERP.Domain.Compliance.Events;

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

    public void SetParties(string? sellerTin, string? sellerName, string? buyerTin, string? buyerName)
    {
        SellerTin = sellerTin;
        SellerName = sellerName;
        BuyerTin = buyerTin;
        BuyerName = buyerName;
        Touch();
    }

    public void SetTransport(string? transporterTin, string? transportType, string? vehicleNumber, string? driverTin, string? startAddress, string? endAddress)
    {
        TransporterTin = transporterTin;
        TransportType = transportType;
        VehicleNumber = vehicleNumber;
        DriverTin = driverTin;
        StartAddress = startAddress;
        EndAddress = endAddress;
        Touch();
    }

    public void SetGoods(string goodsJson, decimal totalAmount)
    {
        GoodsData = goodsJson;
        TotalAmount = totalAmount;
        Touch();

        RaiseDomainEvent(new WaybillSubmittedEvent
        {
            FiscalDocumentId = FiscalDocumentId,
            WaybillId = Id,
            SellerTin = SellerTin,
            BuyerTin = BuyerTin,
            TotalAmount = totalAmount
        });
    }

    /// <summary>RS.GE save_waybill succeeded: a draft waybill now exists server-side.</summary>
    public void MarkSaved(string? waybillNumber)
    {
        EnsureValidTransition(WaybillStatus.Saved);
        WaybillNumber = waybillNumber;
        Status = WaybillStatus.Saved;
        Touch();
    }

    /// <summary>RS.GE send_waybill succeeded: the waybill is active and goods may move.</summary>
    public void MarkActive(DateTimeOffset activateDate)
    {
        EnsureValidTransition(WaybillStatus.Active);
        Status = WaybillStatus.Active;
        ActivateDate = activateDate;
        Touch();
    }

    public void MarkConfirmed()
    {
        EnsureValidTransition(WaybillStatus.Confirmed);
        Status = WaybillStatus.Confirmed;
        Touch();

        RaiseDomainEvent(new WaybillConfirmedEvent
        {
            FiscalDocumentId = FiscalDocumentId,
            WaybillId = Id,
            WaybillNumber = WaybillNumber
        });
    }

    public void MarkClosed(DateTimeOffset deliveryDate)
    {
        EnsureValidTransition(WaybillStatus.Closed);
        Status = WaybillStatus.Closed;
        DeliveryDate = deliveryDate;
        Touch();
    }

    public void MarkRejected()
    {
        EnsureValidTransition(WaybillStatus.Rejected);
        Status = WaybillStatus.Rejected;
        Touch();
    }

    /// <summary>
    /// Validates that a state transition is legal according to the RS.GE waybill lifecycle:
    ///   Draft -> Saved -> Active -> Confirmed -> Closed
    ///                  \-> Rejected (from Draft, Saved, or Active)
    /// Any transition from a terminal state (Closed, Expired, Rejected) is invalid.
    /// </summary>
    private void EnsureValidTransition(WaybillStatus target)
    {
        var valid = (Status, target) switch
        {
            (WaybillStatus.Draft,     WaybillStatus.Saved)     => true,
            (WaybillStatus.Draft,     WaybillStatus.Rejected)  => true,
            (WaybillStatus.Saved,     WaybillStatus.Active)    => true,
            (WaybillStatus.Saved,     WaybillStatus.Rejected)  => true,
            (WaybillStatus.Active,    WaybillStatus.Confirmed) => true,
            (WaybillStatus.Active,    WaybillStatus.Rejected)  => true,
            (WaybillStatus.Active,    WaybillStatus.Closed)    => true,
            (WaybillStatus.Confirmed, WaybillStatus.Closed)    => true,
            _ => false
        };

        if (!valid)
        {
            throw new InvalidOperationException(
                $"Invalid waybill state transition from {Status} to {target}. " +
                $"Waybill {Id} (document {FiscalDocumentId}) cannot transition to {target} from its current state.");
        }
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}
