using BGA.Application.Repositories;

namespace BGA.Infrastructure.DataAccess.Repositories;

public class UnitOfWork(ApplicationDbContext dbContext, IEventRepository eventRepository, IBookingRepository bookingRepository) : IUnitOfWork
{
    public IEventRepository Events { get; } = eventRepository;
    public IBookingRepository Bookings { get; } = bookingRepository;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.SaveChangesAsync(cancellationToken);
    }
}
