using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Finance;

public class JournalEntryLine : BaseEntity
{
    public Guid JournalEntryId { get; private set; }
    public int LineNumber { get; private set; }
    public Guid AccountId { get; private set; }
    public string? Description { get; private set; }
    public decimal DebitAmount { get; private set; }
    public decimal CreditAmount { get; private set; }

    // Navigation properties
    public JournalEntry JournalEntry { get; private set; } = default!;
    public ChartOfAccount Account { get; private set; } = default!;

    private JournalEntryLine() { }

    public static JournalEntryLine Create(
        Guid journalEntryId,
        int lineNumber,
        Guid accountId,
        decimal debitAmount,
        decimal creditAmount,
        string? description = null)
    {
        return new JournalEntryLine
        {
            JournalEntryId = journalEntryId,
            LineNumber = lineNumber,
            AccountId = accountId,
            DebitAmount = debitAmount,
            CreditAmount = creditAmount,
            Description = description
        };
    }
}
