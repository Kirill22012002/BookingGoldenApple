using BGA.API.Dtos;
using BGA.API.Services.Interfaces;
using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace BGA.API.Controllers;

[ApiController]
[Route("[controller]")]
public class EventsController(IEventService _eventService) : ControllerBase
{
    [HttpGet]
    public ApiResult<List<EventDto>> Get()
    {
        return new ApiResult<List<EventDto>>
        {
            Data = _eventService.GetAll(),
            Success = true,
            StatusCode = HttpStatusCode.OK,
            Message = ""
        };
    }

    [HttpGet("{id:int}")]
    public ApiResult<EventDto> Get([FromRoute] int id)
    {
        return new ApiResult<EventDto>
        {
            Data = _eventService.GetById(id),
            Success = true,
            StatusCode = HttpStatusCode.OK,
            Message = ""
        };
    }

    [HttpPost]
    public ApiResult Add([FromBody] AddEventDto dto)
    {
        _eventService.Create(dto);

        return new ApiResult
        {
            Success = true,
            StatusCode = HttpStatusCode.Created,
            Message = ""
        };
    }

    [HttpPut("{id:int}")]
    public ApiResult Put([FromRoute] int id, [FromBody] PutEventDto dto)
    {
        _eventService.Change(id, dto);

        return new ApiResult
        {
            Success = true,
            StatusCode = HttpStatusCode.NoContent,
            Message = ""
        };
    }

    [HttpDelete("{id:int}")]
    public ApiResult Remove([FromRoute] int id)
    {
        _eventService.Remove(id);
        
        return new ApiResult
        {
            Success = true,
            StatusCode = HttpStatusCode.OK,
            Message = ""
        };
    }
}
