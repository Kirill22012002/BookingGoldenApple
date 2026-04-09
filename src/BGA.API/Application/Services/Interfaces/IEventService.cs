using BGA.API.Infrastructure.Models;

namespace BGA.API.Application.Services.Interfaces;

public interface IEventService
{
    Task<ServiceResponse<PaginatedResult<Event>>> GetAllAsync(string? title, DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ServiceResponse<Event>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResponse<Event>> CreateAsync(Event @event, CancellationToken cancellationToken = default);
    Task<ServiceResponse> UpdateAsync(Event @event, CancellationToken cancellationToken = default);
    Task<ServiceResponse> RemoveAsync(Guid id, CancellationToken cancellationToken = default);
}
