using FluentAssertions;
using GeorgiaERP.Domain.CRM;
using Xunit;

namespace GeorgiaERP.Tests.Domain;

public class CustomerTests
{
    private static Customer NewCustomer() =>
        Customer.Create("C-0001", "Giorgi", "Beridze", "გიორგი", "ბერიძე");

    [Fact]
    public void Create_InitializesActiveWithZeroLoyaltyAndVisits()
    {
        var customer = NewCustomer();

        customer.CustomerNumber.Should().Be("C-0001");
        customer.FirstName.Should().Be("Giorgi");
        customer.LastName.Should().Be("Beridze");
        customer.FirstNameKa.Should().Be("გიორგი");
        customer.IsActive.Should().BeTrue();
        customer.LoyaltyPoints.Should().Be(0);
        customer.TotalPurchases.Should().Be(0);
        customer.TotalVisits.Should().Be(0);
        customer.LastVisitAt.Should().BeNull();
    }

    [Fact]
    public void AddPoints_IncreasesLoyaltyBalance()
    {
        var customer = NewCustomer();

        customer.AddPoints(150);
        customer.AddPoints(50);

        customer.LoyaltyPoints.Should().Be(200);
    }

    [Fact]
    public void DeductPoints_DecreasesLoyaltyBalance()
    {
        var customer = NewCustomer();
        customer.AddPoints(200);

        customer.DeductPoints(75);

        customer.LoyaltyPoints.Should().Be(125);
    }

    [Fact]
    public void RecordVisit_AccumulatesVisitsAndPurchasesAndStampsLastVisit()
    {
        var customer = NewCustomer();

        customer.RecordVisit(25.50m);
        customer.RecordVisit(10.00m);

        customer.TotalVisits.Should().Be(2);
        customer.TotalPurchases.Should().Be(35.50m);
        customer.LastVisitAt.Should().NotBeNull();
    }

    [Fact]
    public void SetContactInfo_StoresPhoneAndEmail()
    {
        var customer = NewCustomer();

        customer.SetContactInfo("+995599123456", "giorgi@example.ge");

        customer.Phone.Should().Be("+995599123456");
        customer.Email.Should().Be("giorgi@example.ge");
    }

    [Fact]
    public void SetCompany_StoresCompanyNameAndTin()
    {
        var customer = NewCustomer();

        customer.SetCompany("Beridze LLC", "405123456");

        customer.CompanyName.Should().Be("Beridze LLC");
        customer.Tin.Should().Be("405123456");
    }

    [Fact]
    public void SetLoyaltyCard_StoresCardNumberAndTier()
    {
        var customer = NewCustomer();

        customer.SetLoyaltyCard("LC-9999", "Gold");

        customer.LoyaltyCardNumber.Should().Be("LC-9999");
        customer.LoyaltyTier.Should().Be("Gold");
    }

    [Fact]
    public void SetConsent_StoresSmsAndEmailFlags()
    {
        var customer = NewCustomer();

        customer.SetConsent(sms: true, email: false);

        customer.ConsentSms.Should().BeTrue();
        customer.ConsentEmail.Should().BeFalse();
    }

    [Fact]
    public void DeactivateAndActivate_TogglesIsActive()
    {
        var customer = NewCustomer();

        customer.Deactivate();
        customer.IsActive.Should().BeFalse();

        customer.Activate();
        customer.IsActive.Should().BeTrue();
    }
}
