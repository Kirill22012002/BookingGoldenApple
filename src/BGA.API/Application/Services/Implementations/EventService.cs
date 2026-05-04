using BGA.API.Application.Services.Interfaces;
using BGA.API.Infrastructure.Models;
using BGA.API.Application.Models;
using BGA.API.Application.Exceptions;
using BGA.API.Infrastructure.DataAccess.Repositories.Interfaces;
using BGA.API.Infrastructure.DataAccess;

namespace BGA.API.Application.Services.Implementations;

public class EventService(
    IEventRepository _eventRepository,
    IUnitOfWork _unitOfWork) : IEventService
{
    public async Task<PaginatedResult<Event>> GetAllAsync(string? title, DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (page < 1) throw new ValidationException(nameof(page), $"{nameof(page)} can be more or equal than 1");
        if (pageSize < 0) throw new ValidationException(nameof(pageSize), $"{nameof(pageSize)} can be more or equal than 0");
        if (from.HasValue && to.HasValue && from.Value > to.Value) throw new ValidationException(nameof(to), $"{nameof(to)} can be more or equal than {nameof(from)}"); ;

        var query = await _eventRepository.GetAllAsync(cancellationToken);
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

        return paginatedResult;
    }

    public async Task<Event> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var @event = await _eventRepository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Event not found");
        return @event;
    }

    public async Task<Event> CreateAsync(Event @event, CancellationToken cancellationToken = default)
    {
        await _eventRepository.CreateAsync(@event, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return @event;
    }

    public async Task UpdateAsync(Guid id, string title, string? description, DateTimeOffset startAt, DateTimeOffset endAt, CancellationToken cancellationToken = default)
    {
        var @event = await _eventRepository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Event not found");

        @event.Title = title;
        @event.Reschedule(startAt, endAt);
        if (description != null) @event.Description = description;

        _eventRepository.Update(@event);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var @event = await _eventRepository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Event not found");
        _eventRepository.Remove(@event);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
