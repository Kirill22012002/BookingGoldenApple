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
    public IActionResult Get()
    {
        var dtos = _eventService.GetAll().MapToDtos().ToList();
        return Ok(dtos);
    }

    [HttpGet("{id:int}")]
    public IActionResult Get([FromRoute][Range(1, int.MaxValue)] int id)
    {
        try
        {
            var dto = _eventService.GetById(id).MapToDto();
            return Ok(dto);
        }
        catch (InvalidOperationException)
        {
            return NotFound(new { Error = $"Event with Id: {id} not found" });
        }
    }

    [HttpPost]
    public IActionResult Add([FromBody] AddEventDto dto)
    {
        var @event = _eventService.Create(dto.MapToEntity());
        var eventDto = @event.MapToDto();
        return CreatedAtAction(nameof(Get), new { id = eventDto.Id}, eventDto);
    }

    [HttpPut("{id:int}")]
    public IActionResult Put([FromRoute][Range(1, int.MaxValue)] int id, [FromBody] PutEventDto dto)
    {
        try
        {
            var @event = dto.MapToEntity(id);
            _eventService.Change(id, @event);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return NotFound(new { Error = $"Event with Id: {id} not found" });
        }
    }

    [HttpDelete("{id:int}")]
    public IActionResult Remove([FromRoute][Range(1, int.MaxValue)] int id)
    {
        try
        {
            _eventService.Remove(id);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return NotFound(new { Error = $"Event with Id: {id} not found" });
        }
    }
}
