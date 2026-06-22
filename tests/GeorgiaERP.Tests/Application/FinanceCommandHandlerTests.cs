using FluentAssertions;
using GeorgiaERP.Application.Finance.Commands;
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
}
