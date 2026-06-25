using GeorgiaERP.Application.Payments;
using GeorgiaERP.Domain.Payments;

namespace GeorgiaERP.Infrastructure.Payments;

public class PaymentGatewayFactory : IPaymentGatewayFactory
{
    private readonly IEnumerable<IPaymentGateway> _gateways;

    public PaymentGatewayFactory(IEnumerable<IPaymentGateway> gateways)
    {
        _gateways = gateways;
    }

    public IPaymentGateway GetGateway(PaymentProvider provider)
    {
        return _gateways.FirstOrDefault(g => g.Provider == provider)
               ?? throw new NotSupportedException($"Payment provider '{provider}' is not configured.");
    }
}
