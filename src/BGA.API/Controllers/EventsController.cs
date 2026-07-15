using BGA.API.Dtos;
using BGA.API.Extensions;
using BGA.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace BGA.API.Controllers;

[ApiController]
[Route("events")]
public sealed class EventsController(
    IEventService _eventService,
    IBookingService _bookingService) : BaseController
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? title, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken cancellationToken,
        [FromQuery][Range(1, int.MaxValue)] int page = 1, [FromQuery][Range(0, int.MaxValue)] int pageSize = 10)
    {
        var response = await _eventService.GetAllAsync(title, from, to, page, pageSize, cancellationToken);
        return Ok(response.MapToDto());
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var response = await _eventService.GetByIdAsync(id, cancellationToken);
        return Ok(response.MapToDto());
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Add([FromBody] AddEventDto dto, CancellationToken cancellationToken)
    {
        var @event = dto.MapToEntity();
        var response = await _eventService.CreateAsync(@event, cancellationToken);
        var responseDto = response.MapToDto();

        return CreatedAtAction(nameof(Get), new { id = responseDto?.Id }, responseDto);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] PutEventDto dto, CancellationToken cancellationToken)
    {
        await _eventService.UpdateAsync(id, dto.Title, dto.Description, dto.StartAt, dto.EndAt, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remove([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await _eventService.RemoveAsync(id, cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpPost("{id:guid}/book")]
    public async Task<IActionResult> Book([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var response = await _bookingService.CreateBookingAsync(id, User.GetRequiredUserId(), cancellationToken);
        var responseDto = response.MapToDto();

        return AcceptedAtAction(
            actionName: nameof(BookingsController.Get),
            controllerName: ControllerName<BookingsController>(),
            routeValues: new { id = responseDto?.Id },
            value: new { id = responseDto?.Id, eventId = responseDto?.EventId, status = responseDto?.Status });
    }
}
