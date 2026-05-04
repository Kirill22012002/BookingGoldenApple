using BGA.API.Infrastructure.Models;

namespace BGA.API.Infrastructure.DataAccess.Repositories.Interfaces;

public interface IEventRepository
{
    Task<IQueryable<Event>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task CreateAsync(Event @event, CancellationToken cancellationToken = default);
    void Update(Event @event);
    void Remove(Event @event);
}
