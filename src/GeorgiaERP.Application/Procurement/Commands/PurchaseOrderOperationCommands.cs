using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Procurement;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Procurement.Commands;

public record ApprovePurchaseOrderCommand(Guid PurchaseOrderId, Guid ApprovedBy) : IRequest<Result>;
public record SendPurchaseOrderCommand(Guid PurchaseOrderId) : IRequest<Result>;
public record CancelPurchaseOrderCommand(Guid PurchaseOrderId) : IRequest<Result>;

public class ApprovePurchaseOrderCommandHandler : IRequestHandler<ApprovePurchaseOrderCommand, Result>
{
    private readonly IAppDbContext _dbContext;
    public ApprovePurchaseOrderCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result> Handle(ApprovePurchaseOrderCommand request, CancellationToken ct)
    {
        var order = await _dbContext.PurchaseOrders.FirstOrDefaultAsync(o => o.Id == request.PurchaseOrderId, ct);
        if (order is null) return Result.Failure("Purchase order not found.");
        if (order.Status != PurchaseOrderStatus.Draft) return Result.Failure("Only draft orders can be approved.");
        order.Approve(request.ApprovedBy);
        await _dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public class SendPurchaseOrderCommandHandler : IRequestHandler<SendPurchaseOrderCommand, Result>
{
    private readonly IAppDbContext _dbContext;
    public SendPurchaseOrderCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result> Handle(SendPurchaseOrderCommand request, CancellationToken ct)
    {
        var order = await _dbContext.PurchaseOrders.FirstOrDefaultAsync(o => o.Id == request.PurchaseOrderId, ct);
        if (order is null) return Result.Failure("Purchase order not found.");
        if (order.Status != PurchaseOrderStatus.Approved) return Result.Failure("Only approved orders can be sent.");
        order.Send();
        await _dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public class CancelPurchaseOrderCommandHandler : IRequestHandler<CancelPurchaseOrderCommand, Result>
{
    private readonly IAppDbContext _dbContext;
    public CancelPurchaseOrderCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result> Handle(CancelPurchaseOrderCommand request, CancellationToken ct)
    {
        var order = await _dbContext.PurchaseOrders.FirstOrDefaultAsync(o => o.Id == request.PurchaseOrderId, ct);
        if (order is null) return Result.Failure("Purchase order not found.");
        if (order.Status is PurchaseOrderStatus.Received or PurchaseOrderStatus.Cancelled)
            return Result.Failure("Cannot cancel a received or already cancelled order.");
        order.Cancel();
        await _dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }
}
