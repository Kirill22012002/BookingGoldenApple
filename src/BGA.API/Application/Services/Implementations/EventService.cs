using BGA.API.Infrastructure.Repositories;
using BGA.API.Application.Services.Interfaces;
using BGA.API.Infrastructure.Models;

namespace BGA.API.Application.Services.Implementations;

public class EventService(EventRepository _repository) : IEventService
{
    public ServiceResult<IEnumerable<Event>> GetAll()
    {
        return new ServiceResult<IEnumerable<Event>>
        {
            Success = true,
            Data = _repository.AsEnumerable()
        };
    }

    public ServiceBaseResult GetById(int id)
    {
        var @event = _repository.FirstOrDefault(x => x.Id == id);
        if (@event is null)
        {
            return new ServiceResult
            {
                Success = false,
                ErrorMessage =  $"Event with Id: {id} not found"
            };
        }

        return new ServiceResult<Event>
        {
            Success = true,
            Data = @event
        };
    }

    public ServiceResult<Event> Create(Event @event)
    {
        _repository.Add(@event);
        return new ServiceResult<Event>
        {
            Success = true,
            Data = @event
        };
    }

    public ServiceResult Change(int id, Event @event)
    {
        var index = _repository.FindIndex(x => x.Id == id);
        if (index != -1)
        {
            _repository[index] = @event;
            return new ServiceResult
            {
                Success = true
            };
        }
        else
        {
            return new ServiceResult
            {
                Success = false,
                ErrorMessage = $"Event with Id: {id} not found"
            };
        }
    }

    public ServiceResult Remove(int id)
    {
        var index = _repository.FindIndex(x => x.Id == id);
        if (index != -1)
        {
            _repository.RemoveAt(index);
            return new ServiceResult
            {
                Success = true
            };
        }
        else
        {
            return new ServiceResult
            {
                Success = false,
                ErrorMessage = $"Event with Id: {id} not found"
            };
        }
    }
}
