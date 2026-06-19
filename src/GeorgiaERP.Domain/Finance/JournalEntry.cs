using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Finance;

public enum JournalEntryStatus
{
    Draft,
    Posted,
    Reversed
}

public class JournalEntry : BaseEntity
{
    public string EntryNumber { get; private set; } = default!;
    public DateTimeOffset EntryDate { get; private set; }
    public string? Description { get; private set; }
    public string? SourceType { get; private set; }
    public Guid? SourceId { get; private set; }
    public JournalEntryStatus Status { get; private set; }
    public decimal TotalDebit { get; private set; }
    public decimal TotalCredit { get; private set; }
    public DateTimeOffset? PostedAt { get; private set; }
    public Guid? PostedBy { get; private set; }
    public Guid? ReversedById { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }

    // Navigation properties
    public ICollection<JournalEntryLine> Lines { get; private set; } = new List<JournalEntryLine>();

    private JournalEntry() { }

    public static JournalEntry Create(string entryNumber, DateTimeOffset entryDate, Guid createdBy, string? description = null)
    {
        return new JournalEntry
        {
            EntryNumber = entryNumber,
            EntryDate = entryDate,
            Description = description,
            Status = JournalEntryStatus.Draft,
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void SetTotals(decimal totalDebit, decimal totalCredit)
    {
        TotalDebit = totalDebit;
        TotalCredit = totalCredit;
    }

    public void SetSource(string? sourceType, Guid? sourceId)
    {
        SourceType = sourceType;
        SourceId = sourceId;
    }

    public void Post(Guid postedBy)
    {
        Status = JournalEntryStatus.Posted;
        PostedBy = postedBy;
        PostedAt = DateTimeOffset.UtcNow;
    }

    public void Reverse(Guid reversalEntryId)
    {
        Status = JournalEntryStatus.Reversed;
        ReversedById = reversalEntryId;
    }
}
