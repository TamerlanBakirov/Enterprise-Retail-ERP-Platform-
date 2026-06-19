using FluentAssertions;
using GeorgiaERP.Domain.POS;
using Xunit;

namespace GeorgiaERP.Tests.Domain;

public class PosPaymentTests
{
    [Fact]
    public void Create_DefaultsToGelCurrency()
    {
        var payment = PosPayment.Create(Guid.NewGuid(), PaymentMethod.Cash, 50m);

        payment.Currency.Should().Be("GEL");
        payment.PaymentMethod.Should().Be(PaymentMethod.Cash);
        payment.Amount.Should().Be(50m);
    }

    [Fact]
    public void SetChange_StoresChangeAmount()
    {
        var payment = PosPayment.Create(Guid.NewGuid(), PaymentMethod.Cash, 100m);

        payment.SetChange(15.50m);

        payment.ChangeAmount.Should().Be(15.50m);
    }

    [Fact]
    public void SetReference_StoresCardReference()
    {
        var payment = PosPayment.Create(Guid.NewGuid(), PaymentMethod.Card, 75m);

        payment.SetReference("AUTH-12345", "POS-TERMINAL-1");

        payment.Reference.Should().Be("AUTH-12345");
        payment.TerminalRef.Should().Be("POS-TERMINAL-1");
    }
}
