using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Finance;

public class BankAccount : BaseEntity
{
    public string AccountName { get; private set; } = default!;
    public string BankName { get; private set; } = default!;
    public string AccountNumber { get; private set; } = default!;
    public string? Iban { get; private set; }
    public string Currency { get; private set; } = "GEL";
    public Guid? GlAccountId { get; private set; }
    public decimal CurrentBalance { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // Navigation properties
    public ChartOfAccount? GlAccount { get; private set; }

    private BankAccount() { }

    public static BankAccount Create(string accountName, string bankName, string accountNumber, string currency = "GEL")
    {
        return new BankAccount
        {
            AccountName = accountName,
            BankName = bankName,
            AccountNumber = accountNumber,
            Currency = currency,
            CurrentBalance = 0,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void SetIban(string? iban) => Iban = iban;
    public void LinkGlAccount(Guid glAccountId) => GlAccountId = glAccountId;
    public void UpdateBalance(decimal amount) => CurrentBalance += amount;
    public void Deactivate() => IsActive = false;
}
