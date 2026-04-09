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

    public async Task<bool> CreateAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        await _dbContext.Bookings.AddAsync(booking, cancellationToken);
        var result = await _dbContext.SaveChangesAsync(cancellationToken);
        return result >= 1;
    }

    public async Task<bool> UpdateAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        _dbContext.Bookings.Update(booking);
        var result = await _dbContext.SaveChangesAsync(cancellationToken);
        return result >= 1;
    }
}
