using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Identity.DTOs;
using MediatR;

namespace GeorgiaERP.Application.Identity.Queries;

public record GetUsersQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    bool? IsActive = null) : IRequest<PagedResult<UserDto>>;
