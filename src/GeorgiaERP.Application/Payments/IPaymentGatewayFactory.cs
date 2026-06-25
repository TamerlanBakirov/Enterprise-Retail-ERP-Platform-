using GeorgiaERP.Domain.Payments;

namespace GeorgiaERP.Application.Payments;

public interface IPaymentGatewayFactory
{
    IPaymentGateway GetGateway(PaymentProvider provider);
}
