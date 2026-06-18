using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Finance;

public enum AccountType
{
    Asset,
    Liability,
    Equity,
    Revenue,
    Expense
}

public enum BalanceType
{
    Debit,
    Credit
}

public class ChartOfAccount : BaseEntity
{
    public string AccountCode { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? NameKa { get; private set; }
    public AccountType AccountType { get; private set; }
    public Guid? ParentId { get; private set; }
    public bool IsHeader { get; private set; }
    public bool IsSystem { get; private set; }
    public BalanceType BalanceType { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // Navigation properties
    public ChartOfAccount? Parent { get; private set; }
    public ICollection<ChartOfAccount> Children { get; private set; } = new List<ChartOfAccount>();

    private ChartOfAccount() { }

    public static ChartOfAccount Create(
        string accountCode,
        string name,
        AccountType accountType,
        BalanceType balanceType,
        string? nameKa = null,
        Guid? parentId = null,
        bool isHeader = false)
    {
        return new ChartOfAccount
        {
            AccountCode = accountCode,
            Name = name,
            NameKa = nameKa,
            AccountType = accountType,
            BalanceType = balanceType,
            ParentId = parentId,
            IsHeader = isHeader,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
