using BGA.Application.Repositories;

namespace BGA.Infrastructure.DataAccess.Repositories;

public class UnitOfWork(
    ApplicationDbContext dbContext,
    IEventRepository eventRepository,
    IBookingRepository bookingRepository,
    IUserRepository userRepository) : IUnitOfWork
{
    public IEventRepository Events { get; } = eventRepository;
    public IBookingRepository Bookings { get; } = bookingRepository;
    public IUserRepository Users { get; } = userRepository;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.SaveChangesAsync(cancellationToken);
    }
}
