namespace BGA.Application.Repositories;

public interface IUnitOfWork
{
    IEventRepository Events { get; }
    IBookingRepository Bookings { get; }
    IUserRepository Users { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
