using BGA.API.Presentation.Dtos;
using BGA.API.Infrastructure.Models;
using BGA.API.Application;

namespace BGA.API.Presentation.Extensions;

public static class EventExtensions
{
    public static Event MapToEntity(this AddEventDto dto)
    {
        return new Event
        {
            Id = Guid.Empty,
            Title = dto.Title ?? "",
            Description = dto.Description,
            StartAt = dto.StartAt,
            EndAt = dto.EndAt
        };
    }

    public static Event MapToEntity(this PutEventDto dto, Guid id)
    {
        return new Event
        {
            Id = id,
            Title = dto.Title ?? "",
            Description = dto.Description,
            StartAt = dto.StartAt,
            EndAt = dto.EndAt
        };
    }

    public static EventDto MapToDto(this Event entity)
    {
        return new EventDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description ?? "",
            StartAt = entity.StartAt,
            EndAt = entity.EndAt
        };
    }

    public static PaginatedResult<EventDto> MapToDto(this PaginatedResult<Event> paginatedResult)
    {
        return new PaginatedResult<EventDto>
        {
            Items = paginatedResult.Items.Select(entity => entity.MapToDto()),
            TotalItems = paginatedResult.TotalItems,
            PageNumber = paginatedResult.PageNumber,
            PageSize = paginatedResult.PageSize
        };
    }

    public static IEnumerable<EventDto> MapToDtos(this IEnumerable<Event> entities)
    {
        return entities
            .Select(entity => entity.MapToDto());
    }
}
