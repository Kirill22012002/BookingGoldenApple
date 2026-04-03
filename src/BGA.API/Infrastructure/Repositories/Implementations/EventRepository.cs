using BGA.API.Infrastructure.Repositories.Interfaces;
using System.Collections.Concurrent;
using BGA.API.Infrastructure.Models;

namespace BGA.API.Infrastructure.Repositories.Implementations;

public class EventRepository : IEventRepository
{
    private readonly ConcurrentDictionary<Guid, Event> _events = [];

    public IQueryable<Event> GetAll()
    {
        return _events.Values.AsQueryable();
    }

    public Event GetById(Guid id)
    {
        var @event = _events.GetValueOrDefault(id);
        if (@event == null)
        {
            throw new KeyNotFoundException($"Event with Id: {id} not found");
        }

        return @event;
    }

    public bool Create(Event @event)
    {
        @event.Id = Guid.NewGuid();
        return _events.TryAdd(@event.Id, @event);
    }

    public bool Update(Guid id, Event @event)
    {
        var oldEvent = _events.GetValueOrDefault(id);
        if (oldEvent == null)
        {
            throw new KeyNotFoundException($"Event with Id: {id} not found");
        }

        return _events.TryUpdate(id, @event, oldEvent);
    }

    public bool Remove(Guid id)
    {
        return _events.TryRemove(id, out var _);
    }
}
