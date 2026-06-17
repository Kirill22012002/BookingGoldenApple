using BGA.Domain.Models;

namespace BGA.Application.Services.Interfaces;

public interface IEventService
{
    Task<PaginatedResult<Event>> GetAllAsync(string? title, DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Event> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Event> CreateAsync(Event @event, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, string title, string? description, DateTimeOffset startAt, DateTimeOffset endAt, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid id, CancellationToken cancellationToken = default);
}
