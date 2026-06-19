using FluentAssertions;
using GeorgiaERP.Domain.Licensing;
using Xunit;

namespace GeorgiaERP.Tests.Domain;

public class LicenseTests
{
    private static License NewLicense(DateTimeOffset? expiresAt = null) =>
        License.Create(
            licenseKey: "GERP-TEST-0001",
            companyName: "Acme LLC",
            machineId: "MID-123",
            expiresAt: expiresAt ?? DateTimeOffset.UtcNow.AddYears(1));

    [Fact]
    public void Create_SetsActiveStatusAndDefaults()
    {
        var license = NewLicense();

        license.Status.Should().Be(LicenseStatus.Active);
        license.LicenseKey.Should().Be("GERP-TEST-0001");
        license.CompanyName.Should().Be("Acme LLC");
        license.MachineId.Should().Be("MID-123");
        license.MaxUsers.Should().Be(5);
        license.MaxStores.Should().Be(1);
    }

    [Fact]
    public void Create_WithFutureExpiry_IsValid()
    {
        var license = NewLicense(DateTimeOffset.UtcNow.AddDays(30));

        license.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_IsFalse_WhenExpired()
    {
        var license = NewLicense(DateTimeOffset.UtcNow.AddDays(-1));

        license.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Revoke_MakesLicenseInvalid()
    {
        var license = NewLicense();

        license.Revoke();

        license.Status.Should().Be(LicenseStatus.Revoked);
        license.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Suspend_MakesLicenseInvalid()
    {
        var license = NewLicense();

        license.Suspend();

        license.Status.Should().Be(LicenseStatus.Suspended);
        license.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Renew_ExtendsExpiry()
    {
        var license = NewLicense(DateTimeOffset.UtcNow.AddDays(10));
        var newExpiry = DateTimeOffset.UtcNow.AddYears(1);

        license.Renew(newExpiry);

        license.ExpiresAt.Should().BeCloseTo(newExpiry, TimeSpan.FromSeconds(1));
        license.IsValid.Should().BeTrue();
    }

    [Fact]
    public void SetContactEmail_StoresEmail()
    {
        var license = NewLicense();

        license.SetContactEmail("owner@acme.ge");

        license.ContactEmail.Should().Be("owner@acme.ge");
    }
}
