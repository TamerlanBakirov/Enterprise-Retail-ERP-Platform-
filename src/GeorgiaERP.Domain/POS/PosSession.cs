using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.POS;

public enum PosSessionStatus
{
    Open,
    Closed,
    Reconciled
}

public class PosSession : BaseEntity
{
    public Guid TerminalId { get; private set; }
    public Guid CashierId { get; private set; }
    public DateTimeOffset OpenedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public decimal OpeningBalance { get; private set; }
    public decimal? ClosingBalance { get; private set; }
    public decimal? ExpectedBalance { get; private set; }
    public decimal? CashDifference { get; private set; }
    public PosSessionStatus Status { get; private set; }
    public string? Notes { get; private set; }

    // Navigation properties
    public PosTerminal Terminal { get; private set; } = default!;
    public ICollection<PosTransaction> Transactions { get; private set; } = new List<PosTransaction>();

    private PosSession() { }

    public static PosSession Create(Guid terminalId, Guid cashierId, decimal openingBalance)
    {
        return new PosSession
        {
            TerminalId = terminalId,
            CashierId = cashierId,
            OpeningBalance = openingBalance,
            OpenedAt = DateTimeOffset.UtcNow,
            Status = PosSessionStatus.Open
        };
    }
}
