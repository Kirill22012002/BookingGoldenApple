using BGA.API.Presentation.Dtos;
using BGA.API.Infrastructure.Models;

namespace BGA.API.Presentation.Extensions;

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

    public static IEnumerable<EventDto> MapToDtos(this IEnumerable<Event> entities)
    {
        return entities
            .Select(entity => entity.MapToDto());
    }
}
