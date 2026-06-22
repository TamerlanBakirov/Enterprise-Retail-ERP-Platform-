using FluentAssertions;
using GeorgiaERP.Domain.Finance;
using Xunit;

namespace GeorgiaERP.Tests.Domain;

public class JournalEntryTests
{
    private static JournalEntry NewEntry() =>
        JournalEntry.Create("JE-2026-0001", DateTimeOffset.UtcNow, Guid.NewGuid(), "Opening balance");

    [Fact]
    public void Create_StartsInDraftStatus()
    {
        var entry = NewEntry();

        entry.EntryNumber.Should().Be("JE-2026-0001");
        entry.Status.Should().Be(JournalEntryStatus.Draft);
        entry.Description.Should().Be("Opening balance");
        entry.PostedAt.Should().BeNull();
        entry.PostedBy.Should().BeNull();
    }

    [Fact]
    public void SetTotals_StoresDebitAndCredit()
    {
        var entry = NewEntry();

        entry.SetTotals(1180m, 1180m);

        entry.TotalDebit.Should().Be(1180m);
        entry.TotalCredit.Should().Be(1180m);
    }

    [Fact]
    public void DoubleEntry_DebitsEqualCreditsAcrossLines()
    {
        var entry = NewEntry();
        var cashAccount = Guid.NewGuid();
        var revenueAccount = Guid.NewGuid();

        entry.Lines.Add(JournalEntryLine.Create(entry.Id, 1, cashAccount, 1180m, 0m, "Cash received"));
        entry.Lines.Add(JournalEntryLine.Create(entry.Id, 2, revenueAccount, 0m, 1000m, "Sales revenue"));
        entry.Lines.Add(JournalEntryLine.Create(entry.Id, 3, Guid.NewGuid(), 0m, 180m, "VAT payable"));

        var totalDebit = entry.Lines.Sum(l => l.DebitAmount);
        var totalCredit = entry.Lines.Sum(l => l.CreditAmount);

        totalDebit.Should().Be(totalCredit);
        totalDebit.Should().Be(1180m);
    }

    [Fact]
    public void Post_TransitionsToPostedAndStampsAuditFields()
    {
        var entry = NewEntry();
        var postedBy = Guid.NewGuid();

        entry.Post(postedBy);

        entry.Status.Should().Be(JournalEntryStatus.Posted);
        entry.PostedBy.Should().Be(postedBy);
        entry.PostedAt.Should().NotBeNull();
    }

    [Fact]
    public void Reverse_TransitionsToReversedWithLink()
    {
        var entry = NewEntry();
        entry.Post(Guid.NewGuid());
        var reversalId = Guid.NewGuid();

        entry.Reverse(reversalId);

        entry.Status.Should().Be(JournalEntryStatus.Reversed);
        entry.ReversedById.Should().Be(reversalId);
    }

    [Fact]
    public void SetSource_LinksOriginatingDocument()
    {
        var entry = NewEntry();
        var sourceId = Guid.NewGuid();

        entry.SetSource("PosTransaction", sourceId);

        entry.SourceType.Should().Be("PosTransaction");
        entry.SourceId.Should().Be(sourceId);
    }
}
