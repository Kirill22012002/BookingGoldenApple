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
        [FromQuery][Range(1, int.MaxValue)] int page = 1, [FromQuery][Range(0, int.MaxValue)] int pageSize = 10)
    {

        var response = _eventService.GetAll(title, from, to, page, pageSize);

        return response.Succeeded
            ? Ok(response.Data?.MapToDto())
            : response.ValidationErrors.Count > 0
                ? ValidationProblem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "One or more validation errors occured",
                    type: StatusCodes.Status400BadRequest.GetProblemType(),
                    modelStateDictionary: response.ValidationErrors.ToModelStateDictionary())
                : Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Event not found",
                    type: StatusCodes.Status404NotFound.GetProblemType(),
                    detail: string.Join(". ", response.Errors));
    }

    [HttpGet("{id:guid}")]
    public IActionResult Get([FromRoute] Guid id)
    {
        var response = _eventService.GetById(id);

        return response.Succeeded
            ? Ok(response?.Data?.MapToDto())
            : response.ValidationErrors.Count > 0
                ? ValidationProblem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "One or more validation errors occured",
                    type: StatusCodes.Status400BadRequest.GetProblemType(),
                    modelStateDictionary: response.ValidationErrors.ToModelStateDictionary())
                : Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Event not found",
                    type: StatusCodes.Status404NotFound.GetProblemType(),
                    detail: string.Join(". ", response.Errors));
    }

    [HttpPost]
    public IActionResult Add([FromBody] AddEventDto dto)
    {
        var @event = dto.MapToEntity();
        var response = _eventService.Create(@event);
        var responseDto = response.Data?.MapToDto();

        return response.Succeeded
            ? CreatedAtAction(nameof(Get), new { id = responseDto?.Id }, responseDto)
            : response.ValidationErrors.Count > 0
                ? ValidationProblem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "One or more validation errors occured",
                    type: StatusCodes.Status400BadRequest.GetProblemType(),
                    modelStateDictionary: response.ValidationErrors.ToModelStateDictionary())
                : Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Event not found",
                    type: StatusCodes.Status404NotFound.GetProblemType(),
                    detail: string.Join(". ", response.Errors));
    }

    [HttpPut("{id:guid}")]
    public IActionResult Update([FromRoute] Guid id, [FromBody] PutEventDto dto)
    {
        var @event = dto.MapToEntity(id);
        var response = _eventService.Update(id, @event);

        return response.Succeeded
            ? NoContent()
            : response.ValidationErrors.Count > 0
                ? ValidationProblem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "One or more validation errors occured",
                    type: StatusCodes.Status400BadRequest.GetProblemType(),
                    modelStateDictionary: response.ValidationErrors.ToModelStateDictionary())
                : Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Event not found",
                    type: StatusCodes.Status404NotFound.GetProblemType(),
                    detail: string.Join(". ", response.Errors));
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Remove([FromRoute] Guid id)
    {
        var response = _eventService.Remove(id);

        return response.Succeeded
            ? NoContent()
            : response.ValidationErrors.Count > 0
                ? ValidationProblem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "One or more validation errors occured",
                    type: StatusCodes.Status400BadRequest.GetProblemType(),
                    modelStateDictionary: response.ValidationErrors.ToModelStateDictionary())
                : Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Event not found",
                    type: StatusCodes.Status404NotFound.GetProblemType(),
                    detail: string.Join(". ", response.Errors));
    }
}
