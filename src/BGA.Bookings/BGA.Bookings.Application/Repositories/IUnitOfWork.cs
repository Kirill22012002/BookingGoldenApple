namespace BGA.Bookings.Application.Repositories;

public interface IUnitOfWork
{
    IBookingRepository Bookings { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
