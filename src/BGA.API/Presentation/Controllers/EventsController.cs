using BGA.API.Presentation.Dtos;
using BGA.API.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace BGA.API.Presentation.Controllers;

[ApiController]
[Route("[controller]")]
public class EventsController(IEventService _eventService) : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(_eventService.GetAll());
    }

    [HttpGet("{id:int}")]
    public IActionResult Get([FromRoute][Range(1, int.MaxValue)] int id)
    {
        try
        {
            return Ok(_eventService.GetById(id));
        }
        catch (InvalidOperationException)
        {
            return NotFound(new { Error = $"Event with Id: {id} not found" });
        }
    }

    [HttpPost]
    public IActionResult Add([FromBody] AddEventDto dto)
    {
        var eventDto = _eventService.Create(dto);
        return CreatedAtAction(nameof(Get), new { id = eventDto.Id}, eventDto);
    }

    [HttpPut("{id:int}")]
    public IActionResult Put([FromRoute][Range(1, int.MaxValue)] int id, [FromBody] PutEventDto dto)
    {
        try
        {
            _eventService.Change(id, dto);
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
            return Ok();
        }
        catch (InvalidOperationException)
        {
            return NotFound(new { Error = $"Event with Id: {id} not found" });
        }
    }
}
