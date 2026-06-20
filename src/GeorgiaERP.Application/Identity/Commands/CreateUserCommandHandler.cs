using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Identity.DTOs;
using GeorgiaERP.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Identity.Commands;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<UserDto>>
{
    private readonly IAppDbContext _dbContext;
    private readonly IPasswordService _passwordService;

    public CreateUserCommandHandler(IAppDbContext dbContext, IPasswordService passwordService)
    {
        _dbContext = dbContext;
        _passwordService = passwordService;
    }

    public async Task<Result<UserDto>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await _dbContext.Users
            .AnyAsync(u => u.Username == request.Username || u.Email == request.Email, cancellationToken);

        if (existingUser)
            return Result.Failure<UserDto>("A user with this username or email already exists.");

        var passwordHash = _passwordService.HashPassword(request.Password);

        var user = User.Create(
            username: request.Username,
            email: request.Email,
            passwordHash: passwordHash,
            firstName: request.FirstName,
            lastName: request.LastName,
            firstNameKa: request.FirstNameKa,
            lastNameKa: request.LastNameKa,
            phone: request.Phone,
            defaultStoreId: request.DefaultStoreId,
            defaultLanguage: request.DefaultLanguage);

        _dbContext.Users.Add(user);

        if (request.RoleIds.Count > 0)
        {
            var validRoles = await _dbContext.Roles
                .Where(r => request.RoleIds.Contains(r.Id))
                .Select(r => r.Id)
                .ToListAsync(cancellationToken);

            foreach (var roleId in validRoles)
            {
                var userRole = UserRole.Create(user.Id, roleId);
                _dbContext.UserRoles.Add(userRole);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var roles = await _dbContext.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.Role.Code)
            .ToListAsync(cancellationToken);

        var dto = new UserDto(
            user.Id,
            user.Username,
            user.Email,
            user.FirstName,
            user.LastName,
            user.FirstNameKa,
            user.LastNameKa,
            user.Phone,
            user.DefaultStoreId,
            user.DefaultLanguage,
            user.Is2FaEnabled,
            user.IsActive,
            user.LastLoginAt,
            user.CreatedAt,
            roles);

        return Result.Success(dto);
    }
}
