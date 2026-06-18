using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Products.DTOs;
using MediatR;

namespace GeorgiaERP.Application.Products.Queries;

public record GetProductsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    Guid? CategoryId = null,
    bool? IsActive = null) : IRequest<PagedResult<ProductDto>>;

public record GetProductByIdQuery(Guid Id) : IRequest<ProductDto?>;

public record GetCategoriesQuery(
    Guid? ParentId = null,
    bool? IsActive = null) : IRequest<IReadOnlyList<CategoryDto>>;
