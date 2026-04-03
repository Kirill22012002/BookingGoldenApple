using BGA.API.Infrastructure.Models;

namespace BGA.API.Infrastructure.Repositories.Interfaces;

public interface IEventRepository
{
    Task<IQueryable<Event>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> CreateAsync(Event @event, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Event @event, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default);
}
