using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.POS;

public enum DailyClosingStatus
{
    Draft,
    Closed,
    Reconciled
}

public class DailyClosing : BaseEntity
{
    public Guid StoreId { get; private set; }
    public DateTimeOffset ClosingDate { get; private set; }
    public decimal TotalSales { get; private set; }
    public decimal TotalReturns { get; private set; }
    public decimal TotalVat { get; private set; }
    public decimal CashTotal { get; private set; }
    public decimal CardTotal { get; private set; }
    public decimal OtherTotal { get; private set; }
    public int TransactionCount { get; private set; }
    public DailyClosingStatus Status { get; private set; }
    public Guid? ClosedBy { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }

    private DailyClosing() { }

    public static DailyClosing Create(Guid storeId, DateTimeOffset closingDate)
    {
        return new DailyClosing
        {
            StoreId = storeId,
            ClosingDate = closingDate,
            Status = DailyClosingStatus.Draft
        };
    }
}
