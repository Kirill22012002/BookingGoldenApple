using System.Collections.Concurrent;
using BGA.API.Infrastructure.Models;
using BGA.API.Infrastructure.Repositories.Interfaces;

namespace BGA.API.Infrastructure.Repositories.Implementations;

public class EventRepository : IEventRepository
{
    private readonly ConcurrentDictionary<int, Event> _events = [];
    private int _eventId = 0;

    public IEnumerable<Event> GetAll()
    {
        return _events.Values.AsEnumerable();
    }

    public Event GetById(int id)
    {
        var @event = _events.GetValueOrDefault(id);
        if (@event == null)
        {
            throw new KeyNotFoundException();
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

    public void Update(int id, Event @event)
    {
        _events.AddOrUpdate(id, @event, (key, oldValue) => @event);
    }

    public bool Remove(int id)
    {
        return _events.TryRemove(id, out var _);
    }
}
