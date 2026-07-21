using BGA.Events.Application.Repositories;
using BGA.Events.Application.Services.Interfaces;
using BGA.Events.Domain.Exceptions;
using BGA.Events.Domain.Models;

namespace BGA.Events.Application.Services.Implementations;

public sealed class EventService(IUnitOfWork unitOfWork) : IEventService
{
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

    public async Task<Event> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await unitOfWork.Events.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Event not found");
    }

    public async Task<Event> CreateAsync(Event @event, CancellationToken cancellationToken = default)
    {
        await unitOfWork.Events.CreateAsync(@event, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return @event;
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
