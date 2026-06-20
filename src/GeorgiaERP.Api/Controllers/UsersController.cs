using GeorgiaERP.Application.Identity.Commands;
using GeorgiaERP.Application.Identity.DTOs;
using GeorgiaERP.Application.Identity.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeorgiaERP.Api.Controllers;

[Authorize]
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

    [HttpPost]
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

        return CreatedAtAction(nameof(GetUsers), new { id = result.Value!.Id }, result.Value);
    }
}
