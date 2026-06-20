using FluentAssertions;
using GeorgiaERP.Application.Licensing;
using GeorgiaERP.Domain.Licensing;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GeorgiaERP.Tests.Application;

public class LicensingHandlerTests
{
    private sealed class FakeLicenseKeyValidator : ILicenseKeyValidator
    {
        public LicenseKeyValidationResult Validate(string key) => string.IsNullOrWhiteSpace(key)
            ? LicenseKeyValidationResult.Invalid("Missing key")
            : new(true, "Acme LLC", DateTimeOffset.UtcNow.AddYears(1), 5, 1, null);
    }
    private sealed class FakeMachineIdProvider : IMachineIdProvider
    {
        private readonly string _id;
        public FakeMachineIdProvider(string id) => _id = id;
        public string GetMachineId() => _id;
    }

    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"licensing-{Guid.NewGuid()}")
            .Options);

    [Fact]
    public async Task Activate_CreatesActiveLicense_ForOneYear()
    {
        await using var db = NewContext();
        var handler = new ActivateLicenseCommandHandler(db, new FakeMachineIdProvider("MID-1"), new FakeLicenseKeyValidator());

        var result = await handler.Handle(
            new ActivateLicenseCommand("KEY-1", "Acme LLC", "owner@acme.ge"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CompanyName.Should().Be("Acme LLC");

        var saved = await db.Licenses.SingleAsync();
        saved.Status.Should().Be(LicenseStatus.Active);
        saved.MachineId.Should().Be("MID-1");
        saved.ExpiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddYears(1), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Activate_Fails_WhenLicenseKeyMissing()
    {
        await using var db = NewContext();
        var handler = new ActivateLicenseCommandHandler(db, new FakeMachineIdProvider("MID-1"), new FakeLicenseKeyValidator());

        var result = await handler.Handle(
            new ActivateLicenseCommand("", "Acme LLC", null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Activate_ReturnsExisting_WhenMachineAlreadyActive()
    {
        await using var db = NewContext();
        db.Licenses.Add(License.Create("KEY-OLD", "Existing Co", "MID-1", DateTimeOffset.UtcNow.AddMonths(6)));
        await db.SaveChangesAsync();

        var handler = new ActivateLicenseCommandHandler(db, new FakeMachineIdProvider("MID-1"), new FakeLicenseKeyValidator());
        var result = await handler.Handle(
            new ActivateLicenseCommand("KEY-NEW", "Acme LLC", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CompanyName.Should().Be("Existing Co");
        (await db.Licenses.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Activate_Fails_WhenKeyActiveOnAnotherMachine()
    {
        await using var db = NewContext();
        db.Licenses.Add(License.Create("KEY-1", "Acme LLC", "OTHER-MACHINE", DateTimeOffset.UtcNow.AddYears(1)));
        await db.SaveChangesAsync();

        var handler = new ActivateLicenseCommandHandler(db, new FakeMachineIdProvider("MID-1"), new FakeLicenseKeyValidator());
        var result = await handler.Handle(
            new ActivateLicenseCommand("KEY-1", "Acme LLC", null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already activated");
    }

    [Fact]
    public async Task Renew_ExtendsExpiryByOneYear()
    {
        await using var db = NewContext();
        var license = License.Create("KEY-1", "Acme LLC", "MID-1", DateTimeOffset.UtcNow.AddDays(10));
        db.Licenses.Add(license);
        await db.SaveChangesAsync();

        var handler = new RenewLicenseCommandHandler(db, new FakeMachineIdProvider("MID-1"), new FakeLicenseKeyValidator());
        var result = await handler.Handle(new RenewLicenseCommand("KEY-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var saved = await db.Licenses.SingleAsync();
        saved.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow.AddDays(300));
    }

    [Fact]
    public async Task Renew_Fails_WhenLicenseNotFound()
    {
        await using var db = NewContext();
        var handler = new RenewLicenseCommandHandler(db, new FakeMachineIdProvider("MID-1"), new FakeLicenseKeyValidator());

        var result = await handler.Handle(new RenewLicenseCommand("MISSING"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Deactivate_RevokesActiveLicense()
    {
        await using var db = NewContext();
        db.Licenses.Add(License.Create("KEY-1", "Acme LLC", "MID-1", DateTimeOffset.UtcNow.AddYears(1)));
        await db.SaveChangesAsync();

        var handler = new DeactivateLicenseCommandHandler(db, new FakeMachineIdProvider("MID-1"));
        var result = await handler.Handle(new DeactivateLicenseCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await db.Licenses.SingleAsync()).Status.Should().Be(LicenseStatus.Revoked);
    }

    [Fact]
    public async Task Deactivate_Fails_WhenNoActiveLicense()
    {
        await using var db = NewContext();
        var handler = new DeactivateLicenseCommandHandler(db, new FakeMachineIdProvider("MID-1"));

        var result = await handler.Handle(new DeactivateLicenseCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
