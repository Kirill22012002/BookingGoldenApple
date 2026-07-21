namespace BGA.Bookings.Application.Repositories;

public interface IUnitOfWork
{
    IBookingRepository Bookings { get; }
    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
}
