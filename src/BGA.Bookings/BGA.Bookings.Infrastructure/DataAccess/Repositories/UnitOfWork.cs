using BGA.Bookings.Application.Repositories;

namespace BGA.Bookings.Infrastructure.DataAccess.Repositories;

public sealed class UnitOfWork(
    BookingsDbContext dbContext,
    IBookingRepository bookingRepository) : IUnitOfWork
{
    public IBookingRepository Bookings { get; } = bookingRepository;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
