using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Identity.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Identity.Queries;

public record GetUserByIdQuery(Guid UserId) : IRequest<Result<UserDto>>;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Result<UserDto>>
{
    private readonly IAppDbContext _dbContext;

    public GetUserByIdQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<UserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.AsNoTracking()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
            return Result.NotFound<UserDto>("User", request.UserId);

        return Result.Success(new UserDto(
            user.Id, user.Username, user.Email,
            user.FirstName, user.LastName, user.FirstNameKa, user.LastNameKa,
            user.Phone, user.DefaultStoreId, user.DefaultLanguage,
            user.Is2FaEnabled, user.IsActive, user.LastLoginAt, user.CreatedAt,
            user.UserRoles.Select(ur => ur.Role.Code).ToList()));
    }
}
