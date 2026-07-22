using BGA.Bookings.API.Dtos;
using BGA.Bookings.Domain.Models;

namespace BGA.Bookings.API.Extensions;

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
