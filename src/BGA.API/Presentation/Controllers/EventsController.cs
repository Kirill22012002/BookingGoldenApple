using BGA.API.Presentation.Dtos;
using BGA.API.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using BGA.API.Presentation.Extensions;

namespace BGA.API.Presentation.Controllers;

[ApiController]
[Route("[controller]")]
public class EventsController(
    IEventService _eventService,
    IBookingService _bookingService) : BaseController
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? title, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken,
        [FromQuery][Range(1, int.MaxValue)] int page = 1, [FromQuery][Range(0, int.MaxValue)] int pageSize = 10)
    {

        var response = await _eventService.GetAllAsync(title, from, to, page, pageSize, cancellationToken);

        return response.Succeeded
            ? Ok(response.Data?.MapToDto())
            : ProblemResponse(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var response = await _eventService.GetByIdAsync(id, cancellationToken);

        return response.Succeeded
            ? Ok(response?.Data?.MapToDto())
            : ProblemResponse(response);
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] AddEventDto dto, CancellationToken cancellationToken)
    {
        var @event = dto.MapToEntity();
        var response = await _eventService.CreateAsync(@event, cancellationToken);
        var responseDto = response.Data?.MapToDto();

        return response.Succeeded
            ? CreatedAtAction(nameof(Get), new { id = responseDto?.Id }, responseDto)
            : ProblemResponse(response);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] PutEventDto dto, CancellationToken cancellationToken)
    {
        var @event = dto.MapToEntity(id);
        var response = await _eventService.UpdateAsync(@event, cancellationToken);

        return response.Succeeded
            ? NoContent()
            : ProblemResponse(response);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remove([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var response = await _eventService.RemoveAsync(id, cancellationToken);

        return response.Succeeded
            ? NoContent()
            : ProblemResponse(response);
    }

    [HttpPost("{id:guid}/book")]
    public async Task<IActionResult> Book([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var response = await _bookingService.CreateBookingAsync(id, cancellationToken);
        var responseDto = response.Data?.MapToDto();

        return response.Succeeded
            ? AcceptedAtAction(
                actionName: nameof(BookingsController.Get),
                controllerName: ControllerName<BookingsController>(),
                routeValues: new { id = responseDto?.Id },
                value: new { id = responseDto?.Id, eventId = responseDto?.EventId, status = responseDto?.Status })
            : ProblemResponse(response);
    }
}
