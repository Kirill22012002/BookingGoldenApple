using BGA.API.Presentation.Dtos;
using BGA.API.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using BGA.API.Presentation.Extensions;
using BGA.API.Application;
using BGA.API.Infrastructure.Models;

namespace BGA.API.Presentation.Controllers;

[ApiController]
[Route("[controller]")]
public class EventsController(IEventService _eventService) : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll()
    {
        var result = _eventService.GetAll();
        var dtos = result.Data.MapToDtos().ToList();
        
        return Ok(dtos);
    }

    [HttpGet("{id:int}")]
    public IActionResult GetById([FromRoute][Range(1, int.MaxValue)] int id)
    {
        var result = _eventService.GetById(id);

        return result.Success
            ? Ok((result as ServiceResult<Event>)?.Data.MapToDto())
            : NotFound(new { Error = result.ErrorMessage });
    }

    [HttpPost]
    public IActionResult Create([FromBody] AddEventDto dto)
    {
        var result = _eventService.Create(dto.MapToEntity());
        var eventDto = result.Data.MapToDto();

        return CreatedAtAction(nameof(GetById), new { id = eventDto.Id }, eventDto);
    }

    [HttpPut("{id:int}")]
    public IActionResult Change([FromRoute][Range(1, int.MaxValue)] int id, [FromBody] PutEventDto dto)
    {
        var @event = dto.MapToEntity(id);
        var result = _eventService.Change(id, @event);

        return result.Success
            ? NoContent()
            : NotFound(new { Error = result.ErrorMessage });
    }

    [HttpDelete("{id:int}")]
    public IActionResult Remove([FromRoute][Range(1, int.MaxValue)] int id)
    {
        var result = _eventService.Remove(id);

        return result.Success
            ? NoContent()
            : NotFound(new { Error = result.ErrorMessage });
    }
}
