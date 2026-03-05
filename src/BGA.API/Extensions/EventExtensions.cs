using BGA.API.Dtos;
using BGA.API.Models;

namespace BGA.API.Extensions;

public static class EventExtensions
{
    public static Event MapToEntity(this AddEventDto dto)
    {
        return new Event
        {
            Id = 0,
            Title = dto.Title ?? "",
            Description = dto.Description,
            StartAt = dto.StartAt,
            EndAt = dto.EndAt
        };
    }

    public static Event MapToEntity(this PutEventDto dto, int id)
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

    public static List<Event> MapToEntities(this List<EventDto> dtos)
    {
        return dtos
            .Select(dto => new Event
            {
                Id = 0,
                Title = dto.Title,
                Description = dto.Description,
                StartAt = dto.StartAt,
                EndAt = dto.EndAt
            })
            .ToList();
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

    public static List<EventDto> MapToDtos(this List<Event> entities)
    {
        return entities
            .Select(entity => entity.MapToDto())
            .ToList();
    }
}
