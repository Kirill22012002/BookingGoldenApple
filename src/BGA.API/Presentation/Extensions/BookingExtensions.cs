using BGA.API.Infrastructure.Models;
using BGA.API.Presentation.Dtos;

namespace BGA.API.Presentation.Extensions;

public static class BookingExtensions
{
    public static BookingDto MapToDto(this Booking entity)
    {
        return new BookingDto
        {
            Id = entity.Id,
            EventId = entity.EventId,
            Status = entity.Status.GetEnumValue(),
            CreatedAt = entity.CreatedAt,
            ProcessedAt = entity.ProcessedAt
        };
    }
}
