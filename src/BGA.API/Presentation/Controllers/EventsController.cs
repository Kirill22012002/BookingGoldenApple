using BGA.API.Presentation.Dtos;
using BGA.API.Application.Services.Interfaces;
using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace BGA.API.Presentation.Controllers;

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
            Message = "Got all events"
        };
    }

    [HttpGet("{id:int}")]
    public ApiBaseResult Get([FromRoute] int id)
    {
        try
        {
            return new ApiResult<EventDto>
            {
                Data = _eventService.GetById(id),
                Success = true,
                StatusCode = HttpStatusCode.OK,
                Message = "Got event by id"
            };
        }
        catch (InvalidOperationException ex)
        {
            return new ApiResult
            {
                Success = false,
                StatusCode = HttpStatusCode.NotFound,
                Message = $"Could not find event by id: {ex.Message}"
            };
        }
    }

    [HttpPost]
    public ApiBaseResult Add([FromBody] AddEventDto dto) // return ApiResult if model is not valid
    {
        _eventService.Create(dto);

        return new ApiResult
        {
            Success = true,
            StatusCode = HttpStatusCode.Created,
            Message = "Created event"
        };
    }

    [HttpPut("{id:int}")]
    public ApiBaseResult Put([FromRoute] int id, [FromBody] PutEventDto dto) // return ApiResult if model is not valid
    {
        try
        {
            _eventService.Change(id, dto);

            return new ApiResult
            {
                Success = true,
                StatusCode = HttpStatusCode.NoContent,
                Message = "Changed event by id"
            };
        }
        catch (InvalidOperationException ex)
        {
            return new ApiResult
            {
                Success = false,
                StatusCode = HttpStatusCode.NotFound,
                Message = $"Could not find event by id: {ex.Message}"
            };
        }
    }

    [HttpDelete("{id:int}")]
    public ApiBaseResult Remove([FromRoute] int id)
    {
        try
        {
            _eventService.Remove(id);

            return new ApiResult
            {
                Success = true,
                StatusCode = HttpStatusCode.OK,
                Message = "Removed event by id"
            };
        }
        catch (InvalidOperationException ex)
        {
            return new ApiResult
            {
                Success = false,
                StatusCode = HttpStatusCode.NotFound,
                Message = $"Could not find event by id: {ex.Message}"
            };
        }
    }
}
