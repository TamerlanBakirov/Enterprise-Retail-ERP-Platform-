using GeorgiaERP.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Identity.Commands;

public record UpdateUserCommand(
    Guid UserId,
    string? Email,
    string? FirstName,
    string? LastName,
    string? FirstNameKa,
    string? LastNameKa,
    string? Phone,
    Guid? DefaultStoreId,
    string? DefaultLanguage,
    bool? IsActive) : IRequest<Result>;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Result>
{
    private readonly IAppDbContext _dbContext;

    public UpdateUserCommandHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
            return Result.NotFound("User", request.UserId);

        if (request.Email is not null)
        {
            var emailTaken = await _dbContext.Users
                .AnyAsync(u => u.Email == request.Email && u.Id != request.UserId, cancellationToken);
            if (emailTaken)
                return Result.Failure("Email is already taken.", ErrorCodes.EmailTaken);
        }

        user.Update(
            request.Email, request.FirstName, request.LastName,
            request.FirstNameKa, request.LastNameKa, request.Phone,
            request.DefaultStoreId, request.DefaultLanguage, request.IsActive);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
