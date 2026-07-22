using BGA.Events.Application.Caching;
using BGA.Events.Application.Repositories;
using BGA.Events.Application.Services.Interfaces;
using BGA.Events.Domain.Exceptions;
using BGA.Events.Domain.Models;

namespace BGA.Events.Application.Services.Implementations;

public sealed class EventService(IUnitOfWork unitOfWork, ICacheService cacheService) : IEventService
{
    private static readonly TimeSpan EventByIdCacheTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan TopEventsCacheTtl = TimeSpan.FromMinutes(10);
    private const int TopEventsCount = 10;

    public Task<PaginatedResult<Event>> GetAllAsync(
        string? title,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            throw new ValidationException(nameof(page), $"{nameof(page)} can be more or equal than 1");
        }

        if (pageSize < 0)
        {
            throw new ValidationException(nameof(pageSize), $"{nameof(pageSize)} can be more or equal than 0");
        }

        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            throw new ValidationException(nameof(to), $"{nameof(to)} can be more or equal than {nameof(from)}");
        }

        var query = unitOfWork.Events.GetAll(title, from, to);
        var totalItems = query.Count();
        var items = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(new PaginatedResult<Event>
        {
            Items = items,
            TotalItems = totalItems,
            PageNumber = page,
            PageSize = items.Count
        });
    }

    public async Task<IReadOnlyList<Event>> GetTopAsync(CancellationToken cancellationToken = default)
    {
        var cachedTopEvents = await cacheService.GetAsync<List<Event>>(EventCacheKeys.Top10);
        if (cachedTopEvents is not null)
        {
            return cachedTopEvents;
        }

        var topEvents = await unitOfWork.Events.GetTopAsync(TopEventsCount, cancellationToken);
        await cacheService.SetAsync(EventCacheKeys.Top10, topEvents, TopEventsCacheTtl);
        return topEvents;
    }

    public async Task<Event> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = EventCacheKeys.GetById(id);
        var cachedEvent = await cacheService.GetAsync<Event>(cacheKey);
        if (cachedEvent is not null)
        {
            return cachedEvent;
        }

        var @event = await unitOfWork.Events.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Event not found");
        await cacheService.SetAsync(cacheKey, @event, EventByIdCacheTtl);
        return @event;
    }

    public async Task<Event> CreateAsync(Event @event, CancellationToken cancellationToken = default)
    {
        await unitOfWork.Events.CreateAsync(@event, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return @event;
    }

    public async Task<bool> TryReserveSeatsAsync(Guid id, int seatsCount, CancellationToken cancellationToken = default)
    {
        if (seatsCount <= 0)
        {
            throw new ValidationException(nameof(seatsCount), $"{nameof(seatsCount)} can be more than 0");
        }

        var @event = await unitOfWork.Events.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Event not found");
        if (!@event.TryReserveSeats(seatsCount))
        {
            return false;
        }

        unitOfWork.Events.Update(@event);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task UpdateAsync(Guid id, string title, string? description, DateTimeOffset startAt, DateTimeOffset endAt, CancellationToken cancellationToken = default)
    {
        var @event = await unitOfWork.Events.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Event not found");

        @event.Title = title;
        @event.Reschedule(startAt, endAt);
        @event.Description = description;

        unitOfWork.Events.Update(@event);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var @event = await unitOfWork.Events.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Event not found");
        unitOfWork.Events.Remove(@event);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
