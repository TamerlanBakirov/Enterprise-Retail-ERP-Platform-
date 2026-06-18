using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.CRM;

public class Customer : BaseEntity
{
    public string CustomerNumber { get; private set; } = default!;
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string? FirstNameKa { get; private set; }
    public string? LastNameKa { get; private set; }
    public string? CompanyName { get; private set; }
    public string? Tin { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public DateTimeOffset? DateOfBirth { get; private set; }
    public string? Gender { get; private set; }
    public string? LoyaltyCardNumber { get; private set; }
    public string? LoyaltyTier { get; private set; }
    public int LoyaltyPoints { get; private set; }
    public decimal TotalPurchases { get; private set; }
    public int TotalVisits { get; private set; }
    public DateTimeOffset? LastVisitAt { get; private set; }
    public bool IsActive { get; private set; }
    public bool ConsentSms { get; private set; }
    public bool ConsentEmail { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Navigation properties
    public ICollection<LoyaltyTransaction> LoyaltyTransactions { get; private set; } = new List<LoyaltyTransaction>();

    private Customer() { }

    public static Customer Create(string customerNumber, string firstName, string lastName, string? firstNameKa = null, string? lastNameKa = null)
    {
        return new Customer
        {
            CustomerNumber = customerNumber,
            FirstName = firstName,
            LastName = lastName,
            FirstNameKa = firstNameKa,
            LastNameKa = lastNameKa,
            IsActive = true,
            LoyaltyPoints = 0,
            TotalPurchases = 0,
            TotalVisits = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
