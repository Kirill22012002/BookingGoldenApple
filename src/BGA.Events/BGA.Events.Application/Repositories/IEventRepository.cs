using BGA.Events.Domain.Models;

namespace BGA.Events.Application.Repositories;

public interface IEventRepository
{
    IQueryable<Event> GetAll(string? title, DateTimeOffset? from, DateTimeOffset? to);
    Task<IReadOnlyList<Event>> GetTopAsync(int count, CancellationToken cancellationToken = default);
    Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task CreateAsync(Event @event, CancellationToken cancellationToken = default);
    void Update(Event @event);
    void Remove(Event @event);
}
