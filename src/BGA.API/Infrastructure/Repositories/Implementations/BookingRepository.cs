using BGA.API.Infrastructure.Models;
using BGA.API.Infrastructure.Models.Enums;
using BGA.API.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BGA.API.Infrastructure.Repositories.Implementations;

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
            .ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        await _dbContext.Bookings.AddAsync(booking, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        _dbContext.Bookings.Update(booking);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
