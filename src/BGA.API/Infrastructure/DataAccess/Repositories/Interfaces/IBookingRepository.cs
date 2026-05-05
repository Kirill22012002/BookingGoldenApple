using BGA.API.Infrastructure.Models;

namespace BGA.API.Infrastructure.DataAccess.Repositories.Interfaces;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Booking>> GetAllInPendingAsync(CancellationToken cancellationToken = default);
    Task CreateAsync(Booking booking, CancellationToken cancellationToken = default);
    void Update(Booking booking);
}
