using BGA.Domain.Models;

namespace BGA.Application.Repositories;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Booking>> GetAllInPendingAsync(CancellationToken cancellationToken = default);
    Task CreateAsync(Booking booking, CancellationToken cancellationToken = default);
    void Update(Booking booking);
}
