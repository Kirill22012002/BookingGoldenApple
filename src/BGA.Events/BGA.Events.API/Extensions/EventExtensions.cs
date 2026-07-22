using BGA.Events.API.Dtos;
using BGA.Events.Domain.Models;

namespace BGA.Events.API.Extensions;

public static class EventExtensions
{
    public static Event MapToEntity(this AddEventDto dto)
    {
        return new Event(dto.Title, dto.Description, dto.StartAt, dto.EndAt, dto.TotalSeats);
    }

    public static EventDto MapToDto(this Event entity)
    {
        return new EventDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            StartAt = entity.StartAt,
            EndAt = entity.EndAt,
            TotalSeats = entity.TotalSeats,
            AvailableSeats = entity.AvailableSeats
        };
    }

    public static IReadOnlyList<EventDto> MapToDto(this IEnumerable<Event> entities)
    {
        return entities.Select(MapToDto).ToList();
    }

    public static PaginatedResult<EventDto> MapToDto(this PaginatedResult<Event> paginatedResult)
    {
        return new PaginatedResult<EventDto>
        {
            Items = paginatedResult.Items.Select(MapToDto),
            TotalItems = paginatedResult.TotalItems,
            PageNumber = paginatedResult.PageNumber,
            PageSize = paginatedResult.PageSize
        };
    }
}
