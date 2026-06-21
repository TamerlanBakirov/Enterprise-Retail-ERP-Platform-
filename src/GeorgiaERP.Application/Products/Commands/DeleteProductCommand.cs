using GeorgiaERP.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Products.Commands;

public record DeleteProductCommand(Guid Id) : IRequest<Result>, ICacheInvalidator
{
    public IReadOnlyList<string> CacheKeysToInvalidate =>
        [$"products:id:{Id}", "dashboard:kpi"];
}

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, Result>
{
    private readonly IAppDbContext _dbContext;

    public DeleteProductCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (product is null)
            return Result.Failure("Product not found.", "NOT_FOUND");

        // Soft-delete: deactivate rather than remove, to preserve referential integrity
        // (stock levels, transaction history, etc. reference this product).
        product.Deactivate();
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
