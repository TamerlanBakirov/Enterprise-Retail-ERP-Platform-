using GeorgiaERP.Application.Identity.Commands;
using GeorgiaERP.Application.Identity.DTOs;
using GeorgiaERP.Application.Identity.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GeorgiaERP.Api.Controllers;

[Authorize]
[Tags("Users")]
[EnableRateLimiting("read")]
public class UsersController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null)
    {
        var result = await _mediator.Send(new GetUsersQuery(page, pageSize, search, isActive));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var result = await _mediator.Send(new GetUserByIdQuery(id));
        return ToActionResult(result);
    }

    [HttpPost]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var command = new CreateUserCommand(
            request.Username,
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            request.FirstNameKa,
            request.LastNameKa,
            request.Phone,
            request.DefaultStoreId,
            request.DefaultLanguage,
            request.RoleIds);

        var result = await _mediator.Send(command);

        if (result.IsFailure)
            return ToActionResult(result);

        return CreatedAtAction(nameof(GetUserById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
    {
        var result = await _mediator.Send(new UpdateUserCommand(
            id, request.Email, request.FirstName, request.LastName,
            request.FirstNameKa, request.LastNameKa, request.Phone,
            request.DefaultStoreId, request.DefaultLanguage, request.IsActive));

        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/reset-password")]
    [Authorize(Roles = "super_admin,admin")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> AdminResetPassword(Guid id, [FromBody] AdminResetPasswordRequest request)
    {
        var result = await _mediator.Send(new AdminResetPasswordCommand(id, request.NewPassword));
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/unlock")]
    [Authorize(Roles = "super_admin,admin")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> UnlockAccount(Guid id)
    {
        var result = await _mediator.Send(new UnlockAccountCommand(id));
        return ToActionResult(result);
    }
}

public record AdminResetPasswordRequest(string NewPassword);
