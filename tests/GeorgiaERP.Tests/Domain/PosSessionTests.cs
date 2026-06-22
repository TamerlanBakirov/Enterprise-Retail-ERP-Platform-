using FluentAssertions;
using GeorgiaERP.Domain.POS;
using Xunit;

namespace GeorgiaERP.Tests.Domain;

public class PosSessionTests
{
    [Fact]
    public void Create_OpensSessionWithOpeningBalance()
    {
        var session = PosSession.Create(Guid.NewGuid(), Guid.NewGuid(), 200m);

        session.Status.Should().Be(PosSessionStatus.Open);
        session.OpeningBalance.Should().Be(200m);
        session.ClosedAt.Should().BeNull();
        session.ClosingBalance.Should().BeNull();
    }

    [Fact]
    public void Close_WithMatchingBalance_HasZeroDifference()
    {
        var session = PosSession.Create(Guid.NewGuid(), Guid.NewGuid(), 200m);

        session.Close(closingBalance: 950m, expectedBalance: 950m);

        session.Status.Should().Be(PosSessionStatus.Closed);
        session.ClosingBalance.Should().Be(950m);
        session.ExpectedBalance.Should().Be(950m);
        session.CashDifference.Should().Be(0m);
        session.ClosedAt.Should().NotBeNull();
    }

    [Fact]
    public void Close_WithCashOverage_HasPositiveDifference()
    {
        var session = PosSession.Create(Guid.NewGuid(), Guid.NewGuid(), 200m);

        session.Close(closingBalance: 1000m, expectedBalance: 950m);

        session.CashDifference.Should().Be(50m);
    }

    [Fact]
    public void Close_WithCashShortage_HasNegativeDifference()
    {
        var session = PosSession.Create(Guid.NewGuid(), Guid.NewGuid(), 200m);

        session.Close(closingBalance: 900m, expectedBalance: 950m);

        session.CashDifference.Should().Be(-50m);
    }

    [Fact]
    public void SetNotes_StoresReconciliationNote()
    {
        var session = PosSession.Create(Guid.NewGuid(), Guid.NewGuid(), 200m);

        session.SetNotes("Drawer recounted twice");

        session.Notes.Should().Be("Drawer recounted twice");
    }
}
