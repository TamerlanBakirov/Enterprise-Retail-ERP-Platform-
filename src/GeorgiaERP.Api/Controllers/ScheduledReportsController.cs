using GeorgiaERP.Application.Reports.Commands;
using GeorgiaERP.Application.Reports.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeorgiaERP.Api.Controllers;

[Authorize]
[Route("api/v1/scheduled-reports")]
public class ScheduledReportsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public ScheduledReportsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] bool? isActive = null)
    {
        var result = await _mediator.Send(new GetScheduledReportsQuery(page, pageSize, isActive));
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateScheduledReportCommand command)
    {
        var result = await _mediator.Send(command with { CreatedBy = CurrentUserId });
        return ToActionResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateScheduledReportCommand command)
    {
        var result = await _mediator.Send(command with { Id = id });
        return ToActionResult(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteScheduledReportCommand(id));
        return ToActionResult(result);
    }
}
