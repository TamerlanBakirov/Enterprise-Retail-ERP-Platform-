using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Payments.DTOs;
using GeorgiaERP.Domain.Payments;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Payments.Commands;

public record ProcessPaymentCallbackCommand(
    string ExternalTransactionId,
    string Status,
    string? ErrorMessage = null) : IRequest<Result<PaymentTransactionDto>>;

public class ProcessPaymentCallbackCommandHandler
    : IRequestHandler<ProcessPaymentCallbackCommand, Result<PaymentTransactionDto>>
{
    private readonly IAppDbContext _dbContext;

    public ProcessPaymentCallbackCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PaymentTransactionDto>> Handle(
        ProcessPaymentCallbackCommand request, CancellationToken cancellationToken)
    {
        var transaction = await _dbContext.PaymentTransactions
            .FirstOrDefaultAsync(t => t.ExternalTransactionId == request.ExternalTransactionId, cancellationToken);

        if (transaction is null)
            return Result.NotFound<PaymentTransactionDto>("PaymentTransaction", request.ExternalTransactionId);

        switch (request.Status.ToUpperInvariant())
        {
            case "COMPLETED":
            case "SUCCESS":
                transaction.MarkCompleted();
                break;

            case "FAILED":
            case "ERROR":
                transaction.MarkFailed(request.ErrorMessage ?? "Payment failed.");
                break;

            case "CANCELLED":
                transaction.MarkCancelled();
                break;

            default:
                return Result.Failure<PaymentTransactionDto>($"Unknown payment status: '{request.Status}'.");
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(ToDto(transaction));
    }

    private static PaymentTransactionDto ToDto(PaymentTransaction t) => new(
        t.Id, t.OrderId, t.Amount, t.Currency,
        t.Provider.ToString(), t.Status.ToString(),
        t.ExternalTransactionId, t.ErrorMessage,
        t.CreatedAt, t.CompletedAt, t.Metadata);
}
