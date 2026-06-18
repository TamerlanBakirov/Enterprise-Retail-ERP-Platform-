using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.CRM;

public enum LoyaltyTransactionType
{
    Earn,
    Redeem,
    Adjust,
    Expire
}

public class LoyaltyTransaction : BaseEntity
{
    public Guid CustomerId { get; private set; }
    public LoyaltyTransactionType TransactionType { get; private set; }
    public int Points { get; private set; }
    public string? ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public string? Description { get; private set; }
    public int BalanceAfter { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // Navigation properties
    public Customer Customer { get; private set; } = default!;

    private LoyaltyTransaction() { }

    public static LoyaltyTransaction Create(
        Guid customerId,
        LoyaltyTransactionType transactionType,
        int points,
        int balanceAfter,
        string? description = null)
    {
        return new LoyaltyTransaction
        {
            CustomerId = customerId,
            TransactionType = transactionType,
            Points = points,
            BalanceAfter = balanceAfter,
            Description = description,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
