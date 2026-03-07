using BGA.API.Infrastructure.Repositories;
using BGA.API.Application.Services.Interfaces;
using BGA.API.Infrastructure.Models;

namespace BGA.API.Application.Services.Implementations;

public class EventService(EventRepository _repository) : IEventService
{
    public IEnumerable<Event> GetAll()
    {
        return _repository.AsEnumerable();
    }

    public Event GetById(int id)
    {
        return _repository.Single(x => x.Id == id);
    }

    public Event Create(Event @event)
    {
        _repository.Add(@event);
        return @event;
    }

    public void Change(int id, Event @event)
    {
        var index = _repository.FindIndex(x => x.Id == id);
        if (index != -1)
        {
            _repository[index] = @event;
        }
        else
        {
            throw new InvalidOperationException();
        }
    }

    public void Remove(int id)
    {
        var index = _repository.FindIndex(x => x.Id == id);
        if (index != -1)
        {
            _repository.RemoveAt(index);
        }
        else
        {
            throw new InvalidOperationException();
        }
    }
}
