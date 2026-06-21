using FluentAssertions;
using GeorgiaERP.Domain.CRM;
using Xunit;

namespace GeorgiaERP.Tests.Domain;

public class CrmTests
{
    // === Customer ===

    [Fact]
    public void CreateCustomer_SetsDefaultValues()
    {
        var customer = Customer.Create("CUST-001", "Giorgi", "Beridze", "გიორგი", "ბერიძე");

        customer.CustomerNumber.Should().Be("CUST-001");
        customer.FirstName.Should().Be("Giorgi");
        customer.LastName.Should().Be("Beridze");
        customer.FirstNameKa.Should().Be("გიორგი");
        customer.LastNameKa.Should().Be("ბერიძე");
        customer.IsActive.Should().BeTrue();
        customer.LoyaltyPoints.Should().Be(0);
        customer.TotalPurchases.Should().Be(0);
        customer.TotalVisits.Should().Be(0);
        customer.LastVisitAt.Should().BeNull();
    }

    [Fact]
    public void Customer_SetContactInfo_UpdatesFields()
    {
        var customer = Customer.Create("CUST-001", "Giorgi", "Beridze");

        customer.SetContactInfo("+995 555 123456", "giorgi@example.ge");

        customer.Phone.Should().Be("+995 555 123456");
        customer.Email.Should().Be("giorgi@example.ge");
    }

    [Fact]
    public void Customer_SetCompany_UpdatesFields()
    {
        var customer = Customer.Create("CUST-001", "Giorgi", "Beridze");

        customer.SetCompany("Tbilisi Trading LLC", "123456789");

        customer.CompanyName.Should().Be("Tbilisi Trading LLC");
        customer.Tin.Should().Be("123456789");
    }

    [Fact]
    public void Customer_SetPersonalInfo_UpdatesFields()
    {
        var customer = Customer.Create("CUST-001", "Giorgi", "Beridze");
        var dob = new DateTimeOffset(1990, 5, 15, 0, 0, 0, TimeSpan.Zero);

        customer.SetPersonalInfo(dob, "Male");

        customer.DateOfBirth.Should().Be(dob);
        customer.Gender.Should().Be("Male");
    }

    [Fact]
    public void Customer_SetConsent_UpdatesFlags()
    {
        var customer = Customer.Create("CUST-001", "Giorgi", "Beridze");

        customer.SetConsent(true, false);

        customer.ConsentSms.Should().BeTrue();
        customer.ConsentEmail.Should().BeFalse();
    }

    [Fact]
    public void Customer_SetLoyaltyCard_UpdatesCardInfo()
    {
        var customer = Customer.Create("CUST-001", "Giorgi", "Beridze");

        customer.SetLoyaltyCard("LOYALTY-12345", "Gold");

        customer.LoyaltyCardNumber.Should().Be("LOYALTY-12345");
        customer.LoyaltyTier.Should().Be("Gold");
    }

    [Fact]
    public void Customer_AddPoints_IncreasesBalance()
    {
        var customer = Customer.Create("CUST-001", "Giorgi", "Beridze");

        customer.AddPoints(100);
        customer.AddPoints(50);

        customer.LoyaltyPoints.Should().Be(150);
    }

    [Fact]
    public void Customer_DeductPoints_DecreasesBalance()
    {
        var customer = Customer.Create("CUST-001", "Giorgi", "Beridze");
        customer.AddPoints(200);

        customer.DeductPoints(75);

        customer.LoyaltyPoints.Should().Be(125);
    }

    [Fact]
    public void Customer_RecordVisit_UpdatesStatistics()
    {
        var customer = Customer.Create("CUST-001", "Giorgi", "Beridze");

        customer.RecordVisit(150.50m);
        customer.RecordVisit(200m);

        customer.TotalVisits.Should().Be(2);
        customer.TotalPurchases.Should().Be(350.50m);
        customer.LastVisitAt.Should().NotBeNull();
    }

    [Fact]
    public void Customer_DeactivateAndActivate_TogglesStatus()
    {
        var customer = Customer.Create("CUST-001", "Giorgi", "Beridze");

        customer.Deactivate();
        customer.IsActive.Should().BeFalse();

        customer.Activate();
        customer.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Customer_Operations_UpdateTimestamp()
    {
        var customer = Customer.Create("CUST-001", "Giorgi", "Beridze");
        var initialUpdate = customer.UpdatedAt;

        customer.SetContactInfo("+995 555 000000", null);

        customer.UpdatedAt.Should().BeOnOrAfter(initialUpdate);
    }

    // === LoyaltyTransaction ===

    [Fact]
    public void CreateLoyaltyTransaction_Earn_SetsValues()
    {
        var customerId = Guid.NewGuid();

        var tx = LoyaltyTransaction.Create(customerId, LoyaltyTransactionType.Earn, 100, 100, "Welcome bonus");

        tx.CustomerId.Should().Be(customerId);
        tx.TransactionType.Should().Be(LoyaltyTransactionType.Earn);
        tx.Points.Should().Be(100);
        tx.BalanceAfter.Should().Be(100);
        tx.Description.Should().Be("Welcome bonus");
        tx.ReferenceType.Should().BeNull();
        tx.ReferenceId.Should().BeNull();
    }

    [Fact]
    public void CreateLoyaltyTransaction_Redeem_SetsValues()
    {
        var tx = LoyaltyTransaction.Create(Guid.NewGuid(), LoyaltyTransactionType.Redeem, -50, 50, "Redeemed at POS");

        tx.TransactionType.Should().Be(LoyaltyTransactionType.Redeem);
        tx.Points.Should().Be(-50);
        tx.BalanceAfter.Should().Be(50);
    }

    [Fact]
    public void LoyaltyTransaction_SetReference_UpdatesReferenceFields()
    {
        var tx = LoyaltyTransaction.Create(Guid.NewGuid(), LoyaltyTransactionType.Earn, 50, 150);
        var refId = Guid.NewGuid();

        tx.SetReference("PosTransaction", refId);

        tx.ReferenceType.Should().Be("PosTransaction");
        tx.ReferenceId.Should().Be(refId);
    }

    [Theory]
    [InlineData(LoyaltyTransactionType.Earn)]
    [InlineData(LoyaltyTransactionType.Redeem)]
    [InlineData(LoyaltyTransactionType.Adjust)]
    [InlineData(LoyaltyTransactionType.Expire)]
    public void LoyaltyTransaction_AllTypes_CanBeCreated(LoyaltyTransactionType type)
    {
        var tx = LoyaltyTransaction.Create(Guid.NewGuid(), type, 10, 10);

        tx.TransactionType.Should().Be(type);
    }
}
