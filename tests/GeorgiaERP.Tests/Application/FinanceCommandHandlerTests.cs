using FluentAssertions;
using GeorgiaERP.Application.Finance.Commands;
using GeorgiaERP.Application.Finance.Queries;
using GeorgiaERP.Domain.Finance;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GeorgiaERP.Tests.Application;

public class FinanceCommandHandlerTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"finance-{Guid.NewGuid()}")
            .Options);

    private static async Task<(Guid DebitId, Guid CreditId)> SeedAccounts(AppDbContext db)
    {
        var debit = ChartOfAccount.Create("1000", "Cash", AccountType.Asset, BalanceType.Debit);
        var credit = ChartOfAccount.Create("4000", "Revenue", AccountType.Revenue, BalanceType.Credit);
        db.ChartOfAccounts.Add(debit);
        db.ChartOfAccounts.Add(credit);
        await db.SaveChangesAsync();
        return (debit.Id, credit.Id);
    }

    // === CreateJournalEntry ===

    [Fact]
    public async Task CreateJournalEntry_Balanced_ReturnsSuccess()
    {
        await using var db = NewContext();
        var (debitId, creditId) = await SeedAccounts(db);
        var handler = new CreateJournalEntryCommandHandler(db);

        var result = await handler.Handle(new CreateJournalEntryCommand(
            DateTimeOffset.UtcNow,
            "Test entry",
            null, null,
            Guid.NewGuid(),
            [
                new JournalLineInput(debitId, 1000m, 0m, "Cash debit"),
                new JournalLineInput(creditId, 0m, 1000m, "Revenue credit")
            ]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalDebit.Should().Be(1000m);
        result.Value.TotalCredit.Should().Be(1000m);
        (await db.JournalEntries.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreateJournalEntry_Unbalanced_ReturnsFailure()
    {
        await using var db = NewContext();
        var (debitId, creditId) = await SeedAccounts(db);
        var handler = new CreateJournalEntryCommandHandler(db);

        var result = await handler.Handle(new CreateJournalEntryCommand(
            DateTimeOffset.UtcNow,
            "Unbalanced",
            null, null,
            Guid.NewGuid(),
            [
                new JournalLineInput(debitId, 1000m, 0m),
                new JournalLineInput(creditId, 0m, 500m)
            ]), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("balance");
    }

    [Fact]
    public async Task CreateJournalEntry_InvalidAccount_ReturnsFailure()
    {
        await using var db = NewContext();
        var handler = new CreateJournalEntryCommandHandler(db);

        var result = await handler.Handle(new CreateJournalEntryCommand(
            DateTimeOffset.UtcNow,
            "Bad account",
            null, null,
            Guid.NewGuid(),
            [new JournalLineInput(Guid.NewGuid(), 100m, 0m)]),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task CreateJournalEntry_HeaderAccount_ReturnsFailure()
    {
        await using var db = NewContext();
        var headerAccount = ChartOfAccount.Create("0000", "Header", AccountType.Asset, BalanceType.Debit, isHeader: true);
        db.ChartOfAccounts.Add(headerAccount);
        await db.SaveChangesAsync();

        var handler = new CreateJournalEntryCommandHandler(db);
        var result = await handler.Handle(new CreateJournalEntryCommand(
            DateTimeOffset.UtcNow,
            "Header account",
            null, null,
            Guid.NewGuid(),
            [new JournalLineInput(headerAccount.Id, 100m, 0m)]),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    // === CreateAccount ===

    [Fact]
    public async Task CreateAccount_Valid_ReturnsSuccess()
    {
        await using var db = NewContext();
        var handler = new CreateAccountCommandHandler(db);

        var result = await handler.Handle(new CreateAccountCommand(
            "2000", "Liabilities", "ვალდებულებები", "Liability", "Credit", null, false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await db.ChartOfAccounts.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreateAccount_DuplicateCode_ReturnsFailure()
    {
        await using var db = NewContext();
        db.ChartOfAccounts.Add(ChartOfAccount.Create("1000", "Cash", AccountType.Asset, BalanceType.Debit));
        await db.SaveChangesAsync();

        var handler = new CreateAccountCommandHandler(db);
        var result = await handler.Handle(new CreateAccountCommand(
            "1000", "Duplicate", null, "Asset", "Debit", null, false),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
    }

    // === CreateBankAccount ===

    [Fact]
    public async Task CreateBankAccount_Valid_ReturnsSuccess()
    {
        await using var db = NewContext();
        var handler = new CreateBankAccountCommandHandler(db);

        var result = await handler.Handle(new CreateBankAccountCommand(
            "Main Account", "TBC Bank", "GE29TB7523045063700001", "TBCBGE22", "GEL", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await db.BankAccounts.CountAsync()).Should().Be(1);
    }

    // === TrialBalance ===

    [Fact]
    public async Task TrialBalance_EmptyLedger_ReturnsZeros()
    {
        await using var db = NewContext();
        var handler = new TrialBalanceQueryHandler(db);

        var result = await handler.Handle(new TrialBalanceQuery(), CancellationToken.None);

        result.TotalDebit.Should().Be(0);
        result.TotalCredit.Should().Be(0);
        result.IsBalanced.Should().BeTrue();
        result.Lines.Should().BeEmpty();
    }

    [Fact]
    public async Task TrialBalance_WithPostedEntries_AggregatesCorrectly()
    {
        await using var db = NewContext();
        var (debitId, creditId) = await SeedAccounts(db);

        var entry = JournalEntry.Create("JE-TB-001", DateTimeOffset.UtcNow, Guid.NewGuid(), "Test");
        entry.Lines.Add(JournalEntryLine.Create(entry.Id, 1, debitId, 5000m, 0m));
        entry.Lines.Add(JournalEntryLine.Create(entry.Id, 2, creditId, 0m, 5000m));
        entry.SetTotals(5000m, 5000m);
        entry.Post(Guid.NewGuid());
        db.JournalEntries.Add(entry);
        await db.SaveChangesAsync();

        var handler = new TrialBalanceQueryHandler(db);
        var result = await handler.Handle(new TrialBalanceQuery(), CancellationToken.None);

        result.TotalDebit.Should().Be(5000m);
        result.TotalCredit.Should().Be(5000m);
        result.IsBalanced.Should().BeTrue();
        result.Lines.Should().HaveCount(2);
    }

    [Fact]
    public async Task TrialBalance_IgnoresDraftEntries()
    {
        await using var db = NewContext();
        var (debitId, creditId) = await SeedAccounts(db);

        var draft = JournalEntry.Create("JE-DRAFT", DateTimeOffset.UtcNow, Guid.NewGuid(), "Draft");
        draft.Lines.Add(JournalEntryLine.Create(draft.Id, 1, debitId, 1000m, 0m));
        draft.Lines.Add(JournalEntryLine.Create(draft.Id, 2, creditId, 0m, 1000m));
        draft.SetTotals(1000m, 1000m);
        db.JournalEntries.Add(draft);
        await db.SaveChangesAsync();

        var handler = new TrialBalanceQueryHandler(db);
        var result = await handler.Handle(new TrialBalanceQuery(), CancellationToken.None);

        result.Lines.Should().BeEmpty();
    }

    [Fact]
    public async Task TrialBalance_AsOfDate_FiltersCorrectly()
    {
        await using var db = NewContext();
        var (debitId, creditId) = await SeedAccounts(db);

        var oldEntry = JournalEntry.Create("JE-OLD", DateTimeOffset.UtcNow.AddDays(-30), Guid.NewGuid(), "Old");
        oldEntry.Lines.Add(JournalEntryLine.Create(oldEntry.Id, 1, debitId, 2000m, 0m));
        oldEntry.Lines.Add(JournalEntryLine.Create(oldEntry.Id, 2, creditId, 0m, 2000m));
        oldEntry.SetTotals(2000m, 2000m);
        oldEntry.Post(Guid.NewGuid());
        db.JournalEntries.Add(oldEntry);

        var futureEntry = JournalEntry.Create("JE-FUT", DateTimeOffset.UtcNow.AddDays(30), Guid.NewGuid(), "Future");
        futureEntry.Lines.Add(JournalEntryLine.Create(futureEntry.Id, 1, debitId, 3000m, 0m));
        futureEntry.Lines.Add(JournalEntryLine.Create(futureEntry.Id, 2, creditId, 0m, 3000m));
        futureEntry.SetTotals(3000m, 3000m);
        futureEntry.Post(Guid.NewGuid());
        db.JournalEntries.Add(futureEntry);

        await db.SaveChangesAsync();

        var handler = new TrialBalanceQueryHandler(db);
        var result = await handler.Handle(
            new TrialBalanceQuery(DateTimeOffset.UtcNow),
            CancellationToken.None);

        result.TotalDebit.Should().Be(2000m);
        result.TotalCredit.Should().Be(2000m);
    }
}
