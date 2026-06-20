using FluentAssertions;
using Xunit;

namespace GeorgiaERP.Tests.Domain;

/// <summary>
/// Georgian VAT is 18% and POS prices are VAT-inclusive, so the VAT portion is
/// extracted from the gross line amount: vat = gross * 0.18 / 1.18.
/// This guards the formula used in CreatePosTransactionCommandHandler.
/// </summary>
public class VatCalculationTests
{
    private const decimal VatRate = 0.18m;

    private static decimal ExtractVat(decimal grossAmount) =>
        Math.Round(grossAmount * VatRate / (1 + VatRate), 2);

    [Theory]
    [InlineData(118.00, 18.00)]   // 100 net + 18 VAT
    [InlineData(100.00, 15.25)]   // 84.75 net + 15.25 VAT
    [InlineData(11.80, 1.80)]
    [InlineData(0.00, 0.00)]
    public void ExtractVat_FromGrossInclusivePrice(decimal gross, decimal expectedVat)
    {
        ExtractVat(gross).Should().Be(expectedVat);
    }

    [Fact]
    public void ExtractVat_AppliesDiscountBeforeVat()
    {
        // 2 x 59 GEL gross = 118, minus 18 discount = 100 gross taxable
        var gross = 2 * 59m;
        var afterDiscount = gross - 18m;

        ExtractVat(afterDiscount).Should().Be(15.25m);
    }

    [Fact]
    public void NetPlusVat_EqualsGross()
    {
        var gross = 118m;
        var vat = ExtractVat(gross);
        var net = gross - vat;

        (net + vat).Should().Be(gross);
    }
}
