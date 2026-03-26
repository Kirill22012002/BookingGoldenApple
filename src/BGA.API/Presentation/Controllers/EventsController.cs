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
    public IActionResult Get(
        [FromQuery] string? title, [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var response = _eventService.GetAll(title, from, to, page, pageSize);
        return Ok(response.Data);
    }

    [HttpGet("{id:int}")]
    public IActionResult Get([FromRoute][Range(1, int.MaxValue)] int id)
    {
        var response = _eventService.GetById(id);
        return Ok(response.Data);
    }

    [HttpPost]
    public IActionResult Add([FromBody] AddEventDto dto)
    {
        var response = _eventService.Create(dto);
        return CreatedAtAction(nameof(Get), new { id = response?.Data?.Id }, response?.Data);
    }

    [HttpPut("{id:int}")]
    public IActionResult Put([FromRoute][Range(1, int.MaxValue)] int id, [FromBody] PutEventDto dto)
    {
        var response = _eventService.Update(id, dto);
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
