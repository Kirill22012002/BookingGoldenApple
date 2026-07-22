using BGA.Events.API.Dtos;
using BGA.Events.API.Extensions;
using BGA.Events.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace BGA.Events.API.Controllers;

[ApiController]
[Route("events")]
public sealed class EventsController(IEventService eventService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? title,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken,
        [FromQuery][Range(1, int.MaxValue)] int page = 1,
        [FromQuery][Range(0, int.MaxValue)] int pageSize = 10)
    {
        var response = await eventService.GetAllAsync(title, from, to, page, pageSize, cancellationToken);
        return Ok(response.MapToDto());
    }

    [HttpGet("top")]
    public async Task<IActionResult> GetTop(CancellationToken cancellationToken)
    {
        var response = await eventService.GetTopAsync(cancellationToken);
        return Ok(response.MapToDto());
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var response = await eventService.GetByIdAsync(id, cancellationToken);
        return Ok(response.MapToDto());
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Add([FromBody] AddEventDto dto, CancellationToken cancellationToken)
    {
        var @event = dto.MapToEntity();
        var response = await eventService.CreateAsync(@event, cancellationToken);
        var responseDto = response.MapToDto();

        return CreatedAtAction(nameof(GetById), new { id = responseDto.Id }, responseDto);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] PutEventDto dto, CancellationToken cancellationToken)
    {
        await eventService.UpdateAsync(id, dto.Title, dto.Description, dto.StartAt, dto.EndAt, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remove([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await eventService.RemoveAsync(id, cancellationToken);
        return NoContent();
    }
}
