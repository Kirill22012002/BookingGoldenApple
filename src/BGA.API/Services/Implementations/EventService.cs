using BGA.API.Dtos;
using BGA.API.Extensions;
using BGA.API.Repositories;
using BGA.API.Services.Interfaces;

namespace BGA.API.Services.Implementations;

public class EventService(EventRepository _repository) : IEventService
{
    public List<EventDto> GetAll()
    {
        var events = _repository.ToList();
        return events.MapToDtos();
    }

    public EventDto GetById(int id)
    {
        var @event = _repository.Single(x => x.Id == id);
        return @event.MapToDto();
    }

    public void Create(AddEventDto dto)
    {
        var @event = dto.MapToEntity();
        _repository.Add(@event);
    }

    public void Change(int id, PutEventDto dto)
    {
        Remove(id);
        var @event = dto.MapToEntity(id);
        _repository.Add(@event);
    }

    public void Remove(int id)
    {
        _repository.Remove(_repository.Single(x => x.Id == id));
    }
}
