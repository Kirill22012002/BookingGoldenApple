using BGA.API.Presentation.Dtos;
using BGA.API.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using BGA.API.Presentation.Extensions;

namespace BGA.API.Presentation.Controllers;

[ApiController]
[Route("[controller]")]
public class EventsController(IEventService _eventService) : ControllerBase
{
    [HttpGet]
    public IActionResult Get(
        [FromQuery] string? title, [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var response = _eventService.GetAll(title, from, to, page, pageSize);
        return Ok(response?.Data?.MapToDto());
    }

    [HttpGet("{id:int}")]
    public IActionResult Get([FromRoute][Range(1, int.MaxValue)] int id)
    {
        var response = _eventService.GetById(id);

        return response.Succeeded
            ? Ok(response?.Data?.MapToDto())
            : NotFound(); // with problem details $"Event with Id: {id} not found"
    }

    [HttpPost]
    public IActionResult Add([FromBody] AddEventDto dto)
    {
        var @event = dto.MapToEntity();
        var response = _eventService.Create(@event);
        var responseDto = response?.Data?.MapToDto();

        return CreatedAtAction(nameof(Get), new { id = responseDto?.Id }, responseDto);
    }

    [HttpPut("{id:int}")]
    public IActionResult Update([FromRoute][Range(1, int.MaxValue)] int id, [FromBody] PutEventDto dto)
    {
        var @event = dto.MapToEntity(id);
        var response = _eventService.Update(id, @event);

        return response.Succeeded
            ? NoContent()
            : NotFound(); // with problem details $"Event with Id: {id} not found"
    }

    [HttpDelete("{id:int}")]
    public IActionResult Remove([FromRoute][Range(1, int.MaxValue)] int id)
    {
        var response = _eventService.Remove(id);

        return response.Succeeded
            ? NoContent()
            : NotFound(); // with problem details $"Event with Id: {id} not found"
    }
}
