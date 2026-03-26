using BGA.API.Infrastructure.Repositories.Interfaces;
using System.Collections.Concurrent;
using BGA.API.Infrastructure.Models;

namespace BGA.API.Infrastructure.Repositories.Implementations;

public class EventRepository : IEventRepository
{
    private readonly ConcurrentDictionary<int, Event> _events = [];
    private int _eventId = 0;

    public IQueryable<Event> GetAll()
    {
        return _events.Values.AsQueryable();
    }

    public Event GetById(int id)
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
        if (@event.Id <= 0)
        {
            Interlocked.Increment(ref _eventId);
            @event.Id = _eventId;
        }

        return _events.TryAdd(@event.Id, @event);
    }

    public bool Update(int id, Event @event)
    {
        var oldEvent = _events.GetValueOrDefault(id);
        if (oldEvent == null)
        {
            throw new KeyNotFoundException($"Event with Id: {id} not found");
        }

        return _events.TryUpdate(id, @event, oldEvent);
    }

    public bool Remove(int id)
    {
        return _events.TryRemove(id, out var _);
    }
}
