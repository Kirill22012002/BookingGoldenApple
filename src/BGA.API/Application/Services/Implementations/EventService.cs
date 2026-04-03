using BGA.API.Application.Services.Interfaces;
using BGA.API.Infrastructure.Repositories.Interfaces;
using BGA.API.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace BGA.API.Application.Services.Implementations;

public class EventService(IEventRepository _repository) : IEventService
{
    public async Task<ServiceResponse<PaginatedResult<Event>>> GetAllAsync(string? title, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> validationErrors = [];

        if (page < 1) validationErrors.TryAdd(nameof(page), $"{nameof(page)} can be more or equal than 1");
        if (pageSize < 0) validationErrors.TryAdd(nameof(pageSize), $"{nameof(pageSize)} can be more or equal than 0");
        if (from.HasValue && to.HasValue && from.Value > to.Value) validationErrors.TryAdd(nameof(to), $"{nameof(to)} can be more or equal than {nameof(from)}");

        if (validationErrors.Count != 0) return ServiceResponse<PaginatedResult<Event>>.Failure(validationErrors);

        try
        {
            var query = await _repository.GetAllAsync(cancellationToken);
            if (!string.IsNullOrEmpty(title)) query = query.Where(@event => @event.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
            if (from.HasValue) query = query.Where(@event => @event.StartAt >= from);
            if (to.HasValue) query = query.Where(@event => @event.EndAt <= to);

            var filteredCount = query.Count();

            var items = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            var paginatedResult = new PaginatedResult<Event>()
            {
                Items = items.AsEnumerable(),
                TotalItems = filteredCount,
                PageNumber = page,
                PageSize = items.Count()
            };

            return ServiceResponse<PaginatedResult<Event>>.Success(paginatedResult);
        }
        catch (Exception ex)
        {
            return ServiceResponse<PaginatedResult<Event>>.Failure([ex.Message]);
        }
    }

    public async Task<ServiceResponse<Event>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var @event = await _repository.GetByIdAsync(id, cancellationToken);
            return @event != null
                ? ServiceResponse<Event>.Success(@event)
                : ServiceResponse<Event>.Failure(["Event not found"]);
        }
        catch (Exception ex)
        {
            return ServiceResponse<Event>.Failure([ex.Message]);
        }
    }

    public async Task<ServiceResponse<Event>> CreateAsync(Event @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _repository.CreateAsync(@event, cancellationToken);

            return success
                ? ServiceResponse<Event>.Success(@event)
                : ServiceResponse<Event>.Failure(["Cannot create event"]);
        }
        catch (Exception ex)
        {
            return ServiceResponse<Event>.Failure([ex.Message]);
        }
    }

    public async Task<ServiceResponse> UpdateAsync(Event @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _repository.UpdateAsync(@event, cancellationToken);

            return success
                ? ServiceResponse.Success()
                : ServiceResponse.Failure(["Cannot update event"]);
        }
        catch (Exception ex)
        {
            return ServiceResponse.Failure([ex.Message]);
        }
    }

    public async Task<ServiceResponse> RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _repository.RemoveAsync(id, cancellationToken);

            return success
                ? ServiceResponse.Success()
                : ServiceResponse.Failure(["Cannot remove event"]);
        }
        catch (Exception ex)
        {
            return ServiceResponse.Failure([ex.Message]);
        }
    }
}
