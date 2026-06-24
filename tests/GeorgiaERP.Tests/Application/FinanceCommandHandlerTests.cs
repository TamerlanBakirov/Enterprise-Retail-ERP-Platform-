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

    // === Financial statements helpers ===

    private static async Task<(Guid Cash, Guid Revenue, Guid Expense, Guid Equity, Guid Liability)> SeedFullChart(AppDbContext db)
    {
        var cash = ChartOfAccount.Create("1000", "Cash", AccountType.Asset, BalanceType.Debit);
        var revenue = ChartOfAccount.Create("4000", "Sales Revenue", AccountType.Revenue, BalanceType.Credit);
        var expense = ChartOfAccount.Create("5000", "Cost of Goods", AccountType.Expense, BalanceType.Debit);
        var equity = ChartOfAccount.Create("3000", "Owner Capital", AccountType.Equity, BalanceType.Credit);
        var liability = ChartOfAccount.Create("2000", "Accounts Payable", AccountType.Liability, BalanceType.Credit);
        db.ChartOfAccounts.AddRange(cash, revenue, expense, equity, liability);
        await db.SaveChangesAsync();
        return (cash.Id, revenue.Id, expense.Id, equity.Id, liability.Id);
    }

    private static async Task PostEntry(AppDbContext db, string number, DateTimeOffset date,
        params (Guid AccountId, decimal Debit, decimal Credit)[] lines)
    {
        var entry = JournalEntry.Create(number, date, Guid.NewGuid(), number);
        var no = 1;
        decimal totalDebit = 0, totalCredit = 0;
        foreach (var (accountId, debit, credit) in lines)
        {
            entry.Lines.Add(JournalEntryLine.Create(entry.Id, no++, accountId, debit, credit));
            totalDebit += debit;
            totalCredit += credit;
        }
        entry.SetTotals(totalDebit, totalCredit);
        entry.Post(Guid.NewGuid());
        db.JournalEntries.Add(entry);
        await db.SaveChangesAsync();
    }

    // === IncomeStatement ===

    [Fact]
    public async Task IncomeStatement_EmptyLedger_ReturnsZeros()
    {
        await using var db = NewContext();
        var handler = new IncomeStatementQueryHandler(db);

        var result = await handler.Handle(
            new IncomeStatementQuery(DateTimeOffset.UtcNow.AddMonths(-1), DateTimeOffset.UtcNow),
            CancellationToken.None);

        result.TotalRevenue.Should().Be(0);
        result.TotalExpenses.Should().Be(0);
        result.NetProfit.Should().Be(0);
        result.Revenue.Should().BeEmpty();
        result.Expenses.Should().BeEmpty();
    }

    [Fact]
    public async Task IncomeStatement_ComputesNetProfit()
    {
        await using var db = NewContext();
        var (cash, revenue, expense, _, _) = await SeedFullChart(db);
        var now = DateTimeOffset.UtcNow;

        // Sale: cash 1000 / revenue 1000
        await PostEntry(db, "JE-1", now.AddDays(-2),
            (cash, 1000m, 0m), (revenue, 0m, 1000m));
        // Cost: expense 300 / cash 300
        await PostEntry(db, "JE-2", now.AddDays(-1),
            (expense, 300m, 0m), (cash, 0m, 300m));

        var handler = new IncomeStatementQueryHandler(db);
        var result = await handler.Handle(
            new IncomeStatementQuery(now.AddDays(-5), now), CancellationToken.None);

        result.TotalRevenue.Should().Be(1000m);
        result.TotalExpenses.Should().Be(300m);
        result.NetProfit.Should().Be(700m);
        result.Revenue.Should().ContainSingle();
        result.Expenses.Should().ContainSingle();
    }

    [Fact]
    public async Task IncomeStatement_ExcludesOutOfPeriodEntries()
    {
        await using var db = NewContext();
        var (cash, revenue, _, _, _) = await SeedFullChart(db);
        var now = DateTimeOffset.UtcNow;

        await PostEntry(db, "JE-OLD", now.AddDays(-40),
            (cash, 500m, 0m), (revenue, 0m, 500m));
        await PostEntry(db, "JE-IN", now.AddDays(-2),
            (cash, 800m, 0m), (revenue, 0m, 800m));

        var handler = new IncomeStatementQueryHandler(db);
        var result = await handler.Handle(
            new IncomeStatementQuery(now.AddDays(-7), now), CancellationToken.None);

        result.TotalRevenue.Should().Be(800m);
    }

    // === BalanceSheet ===

    [Fact]
    public async Task BalanceSheet_EmptyLedger_IsBalancedZero()
    {
        await using var db = NewContext();
        var handler = new BalanceSheetQueryHandler(db);

        var result = await handler.Handle(new BalanceSheetQuery(), CancellationToken.None);

        result.TotalAssets.Should().Be(0);
        result.TotalLiabilities.Should().Be(0);
        result.TotalEquity.Should().Be(0);
        result.IsBalanced.Should().BeTrue();
    }

    [Fact]
    public async Task BalanceSheet_BalancesWithCurrentEarnings()
    {
        await using var db = NewContext();
        var (cash, revenue, expense, equity, liability) = await SeedFullChart(db);
        var now = DateTimeOffset.UtcNow;

        // Owner injects 2000 capital: cash 2000 / equity 2000
        await PostEntry(db, "JE-CAP", now.AddDays(-5),
            (cash, 2000m, 0m), (equity, 0m, 2000m));
        // Buy on credit: cash 1000 / payable 1000
        await PostEntry(db, "JE-AP", now.AddDays(-4),
            (cash, 1000m, 0m), (liability, 0m, 1000m));
        // Sale: cash 1500 / revenue 1500
        await PostEntry(db, "JE-SALE", now.AddDays(-3),
            (cash, 1500m, 0m), (revenue, 0m, 1500m));
        // Expense: expense 400 / cash 400
        await PostEntry(db, "JE-EXP", now.AddDays(-2),
            (expense, 400m, 0m), (cash, 0m, 400m));

        var handler = new BalanceSheetQueryHandler(db);
        var result = await handler.Handle(new BalanceSheetQuery(now), CancellationToken.None);

        // Assets: cash = 2000 + 1000 + 1500 - 400 = 4100
        result.TotalAssets.Should().Be(4100m);
        result.TotalLiabilities.Should().Be(1000m);
        result.TotalEquity.Should().Be(2000m);
        result.CurrentEarnings.Should().Be(1100m); // 1500 revenue - 400 expense
        result.IsBalanced.Should().BeTrue();
    }

    // === GeneralLedger ===

    [Fact]
    public async Task GeneralLedger_UnknownAccount_ReturnsNull()
    {
        await using var db = NewContext();
        var handler = new GeneralLedgerQueryHandler(db);

        var result = await handler.Handle(
            new GeneralLedgerQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GeneralLedger_ComputesRunningBalance()
    {
        await using var db = NewContext();
        var (cash, revenue, expense, _, _) = await SeedFullChart(db);
        var now = DateTimeOffset.UtcNow;

        await PostEntry(db, "JE-1", now.AddDays(-3), (cash, 1000m, 0m), (revenue, 0m, 1000m));
        await PostEntry(db, "JE-2", now.AddDays(-2), (expense, 200m, 0m), (cash, 0m, 200m));
        await PostEntry(db, "JE-3", now.AddDays(-1), (cash, 500m, 0m), (revenue, 0m, 500m));

        var handler = new GeneralLedgerQueryHandler(db);
        var result = await handler.Handle(new GeneralLedgerQuery(cash), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Lines.Should().HaveCount(3);
        result.OpeningBalance.Should().Be(0m);
        // Cash (debit account): +1000, -200, +500 = 1300
        result.Lines[0].RunningBalance.Should().Be(1000m);
        result.Lines[1].RunningBalance.Should().Be(800m);
        result.Lines[2].RunningBalance.Should().Be(1300m);
        result.ClosingBalance.Should().Be(1300m);
        result.TotalDebit.Should().Be(1500m);
        result.TotalCredit.Should().Be(200m);
    }

    [Fact]
    public async Task GeneralLedger_OpeningBalance_FromPriorActivity()
    {
        await using var db = NewContext();
        var (cash, revenue, _, _, _) = await SeedFullChart(db);
        var now = DateTimeOffset.UtcNow;

        // Prior period activity -> opening balance.
        await PostEntry(db, "JE-PRIOR", now.AddDays(-30), (cash, 700m, 0m), (revenue, 0m, 700m));
        // In-period activity.
        await PostEntry(db, "JE-IN", now.AddDays(-2), (cash, 300m, 0m), (revenue, 0m, 300m));

        var handler = new GeneralLedgerQueryHandler(db);
        var result = await handler.Handle(
            new GeneralLedgerQuery(cash, now.AddDays(-7), now), CancellationToken.None);

        result!.OpeningBalance.Should().Be(700m);
        result.Lines.Should().ContainSingle();
        result.ClosingBalance.Should().Be(1000m);
    }
}
