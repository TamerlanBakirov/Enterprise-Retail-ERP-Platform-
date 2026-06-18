using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Identity.DTOs;
using MediatR;

namespace GeorgiaERP.Application.Identity.Commands;

public record CreateUserCommand(
    string Username,
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? FirstNameKa,
    string? LastNameKa,
    string? Phone,
    Guid? DefaultStoreId,
    string DefaultLanguage,
    List<Guid> RoleIds) : IRequest<Result<UserDto>>;
