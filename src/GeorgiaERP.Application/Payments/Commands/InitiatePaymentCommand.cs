using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Payments.DTOs;
using GeorgiaERP.Domain.Payments;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Payments.Commands;

public record InitiatePaymentCommand(
    Guid OrderId,
    decimal Amount,
    string Currency,
    PaymentProvider Provider,
    string ReturnUrl,
    string? Metadata = null) : IRequest<Result<PaymentTransactionDto>>;

public class InitiatePaymentCommandHandler : IRequestHandler<InitiatePaymentCommand, Result<PaymentTransactionDto>>
{
    private readonly IAppDbContext _dbContext;
    private readonly IPaymentGatewayFactory _gatewayFactory;

    public InitiatePaymentCommandHandler(IAppDbContext dbContext, IPaymentGatewayFactory gatewayFactory)
    {
        _dbContext = dbContext;
        _gatewayFactory = gatewayFactory;
    }

    public async Task<Result<PaymentTransactionDto>> Handle(InitiatePaymentCommand request, CancellationToken cancellationToken)
    {
        var transaction = PaymentTransaction.Create(
            orderId: request.OrderId,
            amount: request.Amount,
            provider: request.Provider,
            currency: request.Currency,
            metadata: request.Metadata);

        _dbContext.PaymentTransactions.Add(transaction);

        var gateway = _gatewayFactory.GetGateway(request.Provider);
        var initResult = await gateway.InitiatePaymentAsync(
            request.Amount, request.Currency, request.OrderId, request.ReturnUrl);

        if (!initResult.IsSuccess)
        {
            transaction.MarkFailed(initResult.ErrorMessage ?? "Payment initiation failed.");
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result.Failure<PaymentTransactionDto>(initResult.ErrorMessage ?? "Payment initiation failed.");
        }

        transaction.MarkProcessing(initResult.ExternalTransactionId!);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(ToDto(transaction));
    }

    private static PaymentTransactionDto ToDto(PaymentTransaction t) => new(
        t.Id, t.OrderId, t.Amount, t.Currency,
        t.Provider.ToString(), t.Status.ToString(),
        t.ExternalTransactionId, t.ErrorMessage,
        t.CreatedAt, t.CompletedAt, t.Metadata);
}
