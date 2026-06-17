namespace BGA.Application.Repositories;

public interface IUnitOfWork
{
    IEventRepository Events { get; }
    IBookingRepository Bookings { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
