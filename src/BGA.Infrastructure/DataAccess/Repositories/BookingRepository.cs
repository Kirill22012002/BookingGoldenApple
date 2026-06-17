using BGA.Application.Repositories;
using BGA.Domain.Models;
using BGA.Domain.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace BGA.Infrastructure.DataAccess.Repositories;

public class BookingRepository(ApplicationDbContext _dbContext) : IBookingRepository
{
    public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Bookings.SingleOrDefaultAsync(booking => booking.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Booking>> GetAllInPendingAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Bookings
            .Where(booking => booking.Status == BookingStatus.Pending)
            .OrderBy(booking => booking.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        await _dbContext.Bookings.AddAsync(booking, cancellationToken);
    }

    public void Update(Booking booking)
    {
        _dbContext.Bookings.Update(booking);
    }
}
