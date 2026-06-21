using FluentAssertions;
using GeorgiaERP.Domain.Finance;
using Xunit;

namespace GeorgiaERP.Tests.Domain;

public class FinanceTests
{
    // === ChartOfAccount ===

    [Fact]
    public void CreateAccount_SetsDefaultValues()
    {
        var account = ChartOfAccount.Create("1000", "Cash", AccountType.Asset, BalanceType.Debit);

        account.AccountCode.Should().Be("1000");
        account.Name.Should().Be("Cash");
        account.AccountType.Should().Be(AccountType.Asset);
        account.BalanceType.Should().Be(BalanceType.Debit);
        account.IsActive.Should().BeTrue();
        account.IsHeader.Should().BeFalse();
        account.IsSystem.Should().BeFalse();
        account.ParentId.Should().BeNull();
        account.NameKa.Should().BeNull();
        account.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void CreateAccount_WithGeorgianName_SetsNameKa()
    {
        var account = ChartOfAccount.Create("2000", "Accounts Payable", AccountType.Liability,
            BalanceType.Credit, nameKa: "გადასახდელი ანგარიშები");

        account.NameKa.Should().Be("გადასახდელი ანგარიშები");
    }

    [Fact]
    public void CreateAccount_WithParent_SetsParentId()
    {
        var parent = ChartOfAccount.Create("1000", "Assets", AccountType.Asset, BalanceType.Debit, isHeader: true);
        var child = ChartOfAccount.Create("1010", "Petty Cash", AccountType.Asset, BalanceType.Debit,
            parentId: parent.Id);

        child.ParentId.Should().Be(parent.Id);
    }

    [Fact]
    public void CreateAccount_AsHeader_SetsIsHeader()
    {
        var account = ChartOfAccount.Create("1000", "Assets", AccountType.Asset, BalanceType.Debit, isHeader: true);

        account.IsHeader.Should().BeTrue();
    }

    [Fact]
    public void DeactivateAccount_SetsInactive()
    {
        var account = ChartOfAccount.Create("1000", "Cash", AccountType.Asset, BalanceType.Debit);

        account.Deactivate();

        account.IsActive.Should().BeFalse();
    }

    [Fact]
    public void ActivateAccount_AfterDeactivation_SetsActive()
    {
        var account = ChartOfAccount.Create("1000", "Cash", AccountType.Asset, BalanceType.Debit);
        account.Deactivate();

        account.Activate();

        account.IsActive.Should().BeTrue();
    }

    [Fact]
    public void MarkSystem_SetsIsSystem()
    {
        var account = ChartOfAccount.Create("1000", "Cash", AccountType.Asset, BalanceType.Debit);

        account.MarkSystem();

        account.IsSystem.Should().BeTrue();
    }

    [Theory]
    [InlineData(AccountType.Asset, BalanceType.Debit)]
    [InlineData(AccountType.Liability, BalanceType.Credit)]
    [InlineData(AccountType.Equity, BalanceType.Credit)]
    [InlineData(AccountType.Revenue, BalanceType.Credit)]
    [InlineData(AccountType.Expense, BalanceType.Debit)]
    public void AccountType_BalanceType_Combinations(AccountType accountType, BalanceType balanceType)
    {
        var account = ChartOfAccount.Create("1000", "Test", accountType, balanceType);

        account.AccountType.Should().Be(accountType);
        account.BalanceType.Should().Be(balanceType);
    }

    // === JournalEntry ===

    [Fact]
    public void CreateJournalEntry_SetsDefaultValues()
    {
        var userId = Guid.NewGuid();
        var entry = JournalEntry.Create("JE-001", DateTimeOffset.UtcNow, userId, "Test entry");

        entry.EntryNumber.Should().Be("JE-001");
        entry.Status.Should().Be(JournalEntryStatus.Draft);
        entry.CreatedBy.Should().Be(userId);
        entry.Description.Should().Be("Test entry");
        entry.PostedAt.Should().BeNull();
        entry.PostedBy.Should().BeNull();
        entry.ReversedById.Should().BeNull();
        entry.TotalDebit.Should().Be(0);
        entry.TotalCredit.Should().Be(0);
    }

    [Fact]
    public void JournalEntry_SetTotals_UpdatesAmounts()
    {
        var entry = JournalEntry.Create("JE-001", DateTimeOffset.UtcNow, Guid.NewGuid());

        entry.SetTotals(1000m, 1000m);

        entry.TotalDebit.Should().Be(1000m);
        entry.TotalCredit.Should().Be(1000m);
    }

    [Fact]
    public void JournalEntry_SetSource_SetsSourceFields()
    {
        var entry = JournalEntry.Create("JE-001", DateTimeOffset.UtcNow, Guid.NewGuid());
        var sourceId = Guid.NewGuid();

        entry.SetSource("PurchaseOrder", sourceId);

        entry.SourceType.Should().Be("PurchaseOrder");
        entry.SourceId.Should().Be(sourceId);
    }

    [Fact]
    public void JournalEntry_Post_SetsPostedStatus()
    {
        var entry = JournalEntry.Create("JE-001", DateTimeOffset.UtcNow, Guid.NewGuid());
        entry.SetTotals(1000m, 1000m); // Must be balanced to post
        var postedBy = Guid.NewGuid();

        entry.Post(postedBy);

        entry.Status.Should().Be(JournalEntryStatus.Posted);
        entry.PostedBy.Should().Be(postedBy);
        entry.PostedAt.Should().NotBeNull();
    }

    [Fact]
    public void JournalEntry_Post_ThrowsWhenUnbalanced()
    {
        var entry = JournalEntry.Create("JE-001", DateTimeOffset.UtcNow, Guid.NewGuid());
        entry.SetTotals(1000m, 500m); // Unbalanced

        var act = () => entry.Post(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*unbalanced*");
    }

    [Fact]
    public void JournalEntry_Post_ThrowsWhenZeroAmounts()
    {
        var entry = JournalEntry.Create("JE-001", DateTimeOffset.UtcNow, Guid.NewGuid());
        // TotalDebit and TotalCredit default to 0

        var act = () => entry.Post(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*zero amounts*");
    }

    [Fact]
    public void JournalEntry_Post_ThrowsWhenAlreadyPosted()
    {
        var entry = JournalEntry.Create("JE-001", DateTimeOffset.UtcNow, Guid.NewGuid());
        entry.SetTotals(500m, 500m);
        entry.Post(Guid.NewGuid());

        var act = () => entry.Post(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot post*");
    }

    [Fact]
    public void JournalEntry_Reverse_SetsReversedStatus()
    {
        var entry = JournalEntry.Create("JE-001", DateTimeOffset.UtcNow, Guid.NewGuid());
        entry.SetTotals(1000m, 1000m);
        entry.Post(Guid.NewGuid());
        var reversalId = Guid.NewGuid();

        entry.Reverse(reversalId);

        entry.Status.Should().Be(JournalEntryStatus.Reversed);
        entry.ReversedById.Should().Be(reversalId);
    }

    [Fact]
    public void JournalEntry_Reverse_ThrowsWhenNotPosted()
    {
        var entry = JournalEntry.Create("JE-001", DateTimeOffset.UtcNow, Guid.NewGuid());

        var act = () => entry.Reverse(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot reverse*");
    }

    [Fact]
    public void JournalEntry_SetTotals_ThrowsOnNegativeValues()
    {
        var entry = JournalEntry.Create("JE-001", DateTimeOffset.UtcNow, Guid.NewGuid());

        var act = () => entry.SetTotals(-100m, 100m);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*negative*");
    }

    // === JournalEntryLine ===

    [Fact]
    public void CreateJournalEntryLine_SetsValues()
    {
        var entryId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var line = JournalEntryLine.Create(entryId, 1, accountId, 500m, 0m, "Debit cash");

        line.JournalEntryId.Should().Be(entryId);
        line.LineNumber.Should().Be(1);
        line.AccountId.Should().Be(accountId);
        line.DebitAmount.Should().Be(500m);
        line.CreditAmount.Should().Be(0m);
        line.Description.Should().Be("Debit cash");
    }

    [Fact]
    public void CreateJournalEntryLine_CreditSide_SetsValues()
    {
        var line = JournalEntryLine.Create(Guid.NewGuid(), 2, Guid.NewGuid(), 0m, 500m, "Credit revenue");

        line.DebitAmount.Should().Be(0m);
        line.CreditAmount.Should().Be(500m);
    }

    [Fact]
    public void JournalEntryLine_ThrowsWhenBothDebitAndCredit()
    {
        var act = () => JournalEntryLine.Create(Guid.NewGuid(), 1, Guid.NewGuid(), 100m, 50m);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*both debit and credit*");
    }

    [Fact]
    public void JournalEntryLine_ThrowsWhenBothZero()
    {
        var act = () => JournalEntryLine.Create(Guid.NewGuid(), 1, Guid.NewGuid(), 0m, 0m);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*must have either*");
    }

    [Fact]
    public void JournalEntryLine_ThrowsOnNegativeDebit()
    {
        var act = () => JournalEntryLine.Create(Guid.NewGuid(), 1, Guid.NewGuid(), -100m, 0m);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*negative*");
    }

    // === BankAccount ===

    [Fact]
    public void CreateBankAccount_SetsDefaultValues()
    {
        var account = BankAccount.Create("Main Account", "Bank of Georgia", "GE12345678");

        account.AccountName.Should().Be("Main Account");
        account.BankName.Should().Be("Bank of Georgia");
        account.AccountNumber.Should().Be("GE12345678");
        account.Currency.Should().Be("GEL");
        account.CurrentBalance.Should().Be(0);
        account.IsActive.Should().BeTrue();
        account.Iban.Should().BeNull();
        account.GlAccountId.Should().BeNull();
    }

    [Fact]
    public void CreateBankAccount_WithCurrency_SetsCurrency()
    {
        var account = BankAccount.Create("USD Account", "TBC Bank", "USD-001", "USD");

        account.Currency.Should().Be("USD");
    }

    [Fact]
    public void BankAccount_SetIban_UpdatesIban()
    {
        var account = BankAccount.Create("Main", "BOG", "12345");

        account.SetIban("GE29TB7090145378900100");

        account.Iban.Should().Be("GE29TB7090145378900100");
    }

    [Fact]
    public void BankAccount_LinkGlAccount_SetsGlAccountId()
    {
        var account = BankAccount.Create("Main", "BOG", "12345");
        var glAccountId = Guid.NewGuid();

        account.LinkGlAccount(glAccountId);

        account.GlAccountId.Should().Be(glAccountId);
    }

    [Fact]
    public void BankAccount_UpdateBalance_AddsAmount()
    {
        var account = BankAccount.Create("Main", "BOG", "12345");

        account.UpdateBalance(1000m);
        account.UpdateBalance(500m);

        account.CurrentBalance.Should().Be(1500m);
    }

    [Fact]
    public void BankAccount_UpdateBalance_SubtractsNegativeAmount()
    {
        var account = BankAccount.Create("Main", "BOG", "12345");
        account.UpdateBalance(1000m);

        account.UpdateBalance(-300m);

        account.CurrentBalance.Should().Be(700m);
    }

    [Fact]
    public void BankAccount_Deactivate_SetsInactive()
    {
        var account = BankAccount.Create("Main", "BOG", "12345");

        account.Deactivate();

        account.IsActive.Should().BeFalse();
    }
}
