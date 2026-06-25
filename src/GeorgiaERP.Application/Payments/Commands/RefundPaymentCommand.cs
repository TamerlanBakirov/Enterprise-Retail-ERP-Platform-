using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Payments.DTOs;
using GeorgiaERP.Domain.Payments;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Payments.Commands;

public record RefundPaymentCommand(
    Guid PaymentTransactionId,
    decimal? Amount = null) : IRequest<Result<PaymentTransactionDto>>;

public class RefundPaymentCommandHandler
    : IRequestHandler<RefundPaymentCommand, Result<PaymentTransactionDto>>
{
    private readonly IAppDbContext _dbContext;
    private readonly IPaymentGatewayFactory _gatewayFactory;

    public RefundPaymentCommandHandler(IAppDbContext dbContext, IPaymentGatewayFactory gatewayFactory)
    {
        _dbContext = dbContext;
        _gatewayFactory = gatewayFactory;
    }

    public async Task<Result<PaymentTransactionDto>> Handle(
        RefundPaymentCommand request, CancellationToken cancellationToken)
    {
        var transaction = await _dbContext.PaymentTransactions
            .FirstOrDefaultAsync(t => t.Id == request.PaymentTransactionId, cancellationToken);

        if (transaction is null)
            return Result.NotFound<PaymentTransactionDto>("PaymentTransaction", request.PaymentTransactionId);

        if (transaction.Status != PaymentStatus.Completed)
            return Result.Failure<PaymentTransactionDto>("Only completed payments can be refunded.");

        var refundAmount = request.Amount ?? transaction.Amount;

        if (transaction.ExternalTransactionId is not null)
        {
            var gateway = _gatewayFactory.GetGateway(transaction.Provider);
            var refundResult = await gateway.RefundAsync(
                transaction.ExternalTransactionId, refundAmount);

            if (!refundResult.IsSuccess)
                return Result.Failure<PaymentTransactionDto>(refundResult.ErrorMessage ?? "Refund failed.");
        }

        transaction.MarkRefunded();
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(ToDto(transaction));
    }

    private static PaymentTransactionDto ToDto(PaymentTransaction t) => new(
        t.Id, t.OrderId, t.Amount, t.Currency,
        t.Provider.ToString(), t.Status.ToString(),
        t.ExternalTransactionId, t.ErrorMessage,
        t.CreatedAt, t.CompletedAt, t.Metadata);
}
