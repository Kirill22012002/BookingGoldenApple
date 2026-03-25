using BGA.API.Presentation.Dtos;
using BGA.API.Application.Extensions;
using BGA.API.Application.Services.Interfaces;
using BGA.API.Infrastructure.Repositories.Interfaces;

namespace BGA.API.Application.Services.Implementations;

public class EventService(IEventRepository _repository) : IEventService
{
    public ServiceResponse<List<EventDto>> GetAll()
    {
        var events = _repository.GetAll();
        return ServiceResponse<List<EventDto>>.Success(events.MapToDtos().ToList());
    }

    public ServiceResponse<EventDto> GetById(int id)
    {
        var @event = _repository.GetById(id);
        return ServiceResponse<EventDto>.Success(@event.MapToDto());
    }

    public ServiceResponse<EventDto> Create(AddEventDto dto)
    {
        var @event = dto.MapToEntity();
        var success = _repository.Create(@event);

        return success
            ? ServiceResponse<EventDto>.Success(@event.MapToDto())
            : ServiceResponse<EventDto>.Failure(["Cannot create event"]);
    }

    public ServiceResponse Change(int id, PutEventDto dto)
    {
        var @event = dto.MapToEntity(id);
        _repository.Update(id, @event);

        return ServiceResponse.Success();
    }

    public ServiceResponse Remove(int id)
    {
        var success = _repository.Remove(id);

        return success
            ? ServiceResponse.Success()
            : ServiceResponse.Failure(["Cannot remove event"]);
    }
}
