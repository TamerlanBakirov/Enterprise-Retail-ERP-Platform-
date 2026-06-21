using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Products.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Products.Commands;

public record UpdateProductCommand(
    Guid Id,
    string? Name,
    string? NameKa,
    string? Description,
    Guid? CategoryId,
    string? UnitOfMeasure,
    bool? VatApplicable,
    decimal? WeightKg,
    decimal? MinStockLevel,
    decimal? MaxStockLevel,
    decimal? ReorderPoint,
    decimal? ReorderQty,
    bool? IsActive) : IRequest<Result>;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Result>
{
    private readonly IAppDbContext _dbContext;

    public UpdateProductCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (product is null)
            return Result.Failure("Product not found.", "NOT_FOUND");

        product.Update(
            request.Name,
            request.NameKa,
            request.Description,
            request.CategoryId,
            request.UnitOfMeasure,
            request.VatApplicable,
            request.WeightKg,
            request.MinStockLevel,
            request.MaxStockLevel,
            request.ReorderPoint,
            request.ReorderQty);

        if (request.IsActive.HasValue)
        {
            if (request.IsActive.Value)
                product.Activate();
            else
                product.Deactivate();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
