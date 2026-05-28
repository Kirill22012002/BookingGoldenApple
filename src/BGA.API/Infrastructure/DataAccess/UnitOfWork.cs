using BGA.API.Infrastructure.DataAccess.Repositories.Interfaces;

namespace BGA.API.Infrastructure.DataAccess;

public interface IUnitOfWork
{
    IEventRepository Events { get; }
    IBookingRepository Bookings { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class UnitOfWork(ApplicationDbContext dbContext, IEventRepository eventRepository, IBookingRepository bookingRepository) : IUnitOfWork
{
    public IEventRepository Events { get; } = eventRepository;
    public IBookingRepository Bookings { get; } = bookingRepository;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.SaveChangesAsync(cancellationToken);
    }
}
