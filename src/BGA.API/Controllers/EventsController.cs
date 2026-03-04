using BGA.API.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace BGA.API.Controllers;

[ApiController]
[Route("[controller]")]
public class EventsController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok();
    }

    [HttpGet("{id:int}")]
    public IActionResult Get([FromRoute] int id)
    {
        return Ok();
    }

    [HttpPost]
    public IActionResult Add([FromBody] AddEventDto dto)
    {
        return Ok();
    }

    [HttpPut("{id:int}")]
    public IActionResult Put([FromRoute] int id, [FromBody] PutEventDto dto)
    {
        return Ok();
    }

    [HttpDelete("{id:int}")]
    public IActionResult Remove([FromRoute] int id)
    {
        return Ok();
    }
}
