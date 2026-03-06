using BGA.API.Presentation.Dtos;
using BGA.API.Application.Extensions;
using BGA.API.Infrastructure.Repositories;
using BGA.API.Application.Services.Interfaces;

namespace BGA.API.Application.Services.Implementations;

public class EventService(EventRepository _repository) : IEventService
{
    public List<EventDto> GetAll()
    {
        var events = _repository.ToList();
        return events.MapToDtos().ToList();
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
