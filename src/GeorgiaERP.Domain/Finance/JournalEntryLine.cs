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
        if (debitAmount < 0) throw new InvalidOperationException("Debit amount cannot be negative.");
        if (creditAmount < 0) throw new InvalidOperationException("Credit amount cannot be negative.");
        if (debitAmount > 0 && creditAmount > 0)
            throw new InvalidOperationException("A journal entry line cannot have both debit and credit amounts.");
        if (debitAmount == 0 && creditAmount == 0)
            throw new InvalidOperationException("A journal entry line must have either a debit or credit amount.");

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
