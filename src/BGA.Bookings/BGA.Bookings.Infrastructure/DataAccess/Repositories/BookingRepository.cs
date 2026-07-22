using BGA.Bookings.Application.Repositories;
using BGA.Bookings.Domain.Models;
using BGA.Bookings.Domain.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace BGA.Bookings.Infrastructure.DataAccess.Repositories;

public sealed class BookingRepository(BookingsDbContext dbContext) : IBookingRepository
{
    public Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Bookings.SingleOrDefaultAsync(booking => booking.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Booking>> GetAllInPendingAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Bookings
            .Where(booking => booking.Status == BookingStatus.Pending)
            .OrderBy(booking => booking.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return dbContext.Bookings.CountAsync(
            booking => booking.UserId == userId &&
                (booking.Status == BookingStatus.Pending || booking.Status == BookingStatus.Confirmed),
            cancellationToken);
    }

    public Task CreateAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        return dbContext.Bookings.AddAsync(booking, cancellationToken).AsTask();
    }

    public void Update(Booking booking)
    {
        dbContext.Bookings.Update(booking);
    }
}
