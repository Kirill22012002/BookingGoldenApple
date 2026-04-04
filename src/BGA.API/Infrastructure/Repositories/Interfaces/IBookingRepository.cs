using BGA.API.Infrastructure.Models;

namespace BGA.API.Infrastructure.Repositories.Interfaces;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Booking>> GetAllInPendingAsync(CancellationToken cancellationToken = default);
    Task<bool> CreateAsync(Booking booking, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Booking booking, CancellationToken cancellationToken = default);
}
