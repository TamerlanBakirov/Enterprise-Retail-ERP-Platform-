using FluentAssertions;
using GeorgiaERP.Domain.Finance;
using Xunit;

namespace GeorgiaERP.Tests.Domain;

public class ChartOfAccountTests
{
    [Fact]
    public void Create_AssetAccount_IsActiveWithDebitBalance()
    {
        var account = ChartOfAccount.Create("1100", "Cash", AccountType.Asset, BalanceType.Debit, "ნაღდი ფული");

        account.AccountCode.Should().Be("1100");
        account.Name.Should().Be("Cash");
        account.NameKa.Should().Be("ნაღდი ფული");
        account.AccountType.Should().Be(AccountType.Asset);
        account.BalanceType.Should().Be(BalanceType.Debit);
        account.IsActive.Should().BeTrue();
        account.IsHeader.Should().BeFalse();
        account.IsSystem.Should().BeFalse();
    }

    [Theory]
    [InlineData(AccountType.Asset, BalanceType.Debit)]
    [InlineData(AccountType.Expense, BalanceType.Debit)]
    [InlineData(AccountType.Liability, BalanceType.Credit)]
    [InlineData(AccountType.Equity, BalanceType.Credit)]
    [InlineData(AccountType.Revenue, BalanceType.Credit)]
    public void Create_SupportsAllAccountTypes(AccountType type, BalanceType balance)
    {
        var account = ChartOfAccount.Create("X", "Account", type, balance);

        account.AccountType.Should().Be(type);
        account.BalanceType.Should().Be(balance);
    }

    [Fact]
    public void Create_HeaderAccount_FlagsHeader()
    {
        var account = ChartOfAccount.Create("1000", "Assets", AccountType.Asset, BalanceType.Debit, isHeader: true);

        account.IsHeader.Should().BeTrue();
    }

    [Fact]
    public void Create_ChildAccount_LinksToParent()
    {
        var parentId = Guid.NewGuid();

        var account = ChartOfAccount.Create("1110", "Petty Cash", AccountType.Asset, BalanceType.Debit, parentId: parentId);

        account.ParentId.Should().Be(parentId);
    }

    [Fact]
    public void DeactivateAndActivate_TogglesIsActive()
    {
        var account = ChartOfAccount.Create("1100", "Cash", AccountType.Asset, BalanceType.Debit);

        account.Deactivate();
        account.IsActive.Should().BeFalse();

        account.Activate();
        account.IsActive.Should().BeTrue();
    }

    [Fact]
    public void MarkSystem_FlagsAccountAsSystemManaged()
    {
        var account = ChartOfAccount.Create("2200", "VAT Payable", AccountType.Liability, BalanceType.Credit);

        account.MarkSystem();

        account.IsSystem.Should().BeTrue();
    }
}
