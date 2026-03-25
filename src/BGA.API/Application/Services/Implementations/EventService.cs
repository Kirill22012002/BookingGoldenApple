using BGA.API.Presentation.Dtos;
using BGA.API.Application.Extensions;
using BGA.API.Application.Services.Interfaces;
using BGA.API.Infrastructure.Repositories.Interfaces;

namespace BGA.API.Application.Services.Implementations;

public class EventService(IEventRepository _repository) : IEventService
{
    public ServiceResponse<List<EventDto>> GetAll()
    {
        try
        {
            var events = _repository.GetAll();
            return ServiceResponse<List<EventDto>>.Success(events.MapToDtos().ToList());
        }
        catch (Exception ex)
        {
            return ServiceResponse<List<EventDto>>.Failure([ex.Message]);
        }
    }

    public ServiceResponse<EventDto> GetById(int id)
    {
        try
        {
            var @event = _repository.GetById(id);
            return ServiceResponse<EventDto>.Success(@event.MapToDto());
        }
        catch (Exception ex)
        {
            return ServiceResponse<EventDto>.Failure([ex.Message]);
        }
    }

    public ServiceResponse<EventDto> Create(AddEventDto dto)
    {
        try
        {
            var @event = dto.MapToEntity();
            var success = _repository.Create(@event);

            return success
                ? ServiceResponse<EventDto>.Success(@event.MapToDto())
                : ServiceResponse<EventDto>.Failure(["Cannot create event"]);

        }
        catch (Exception ex)
        {
            return ServiceResponse<EventDto>.Failure([ex.Message]);
        }
    }

    public ServiceResponse Change(int id, PutEventDto dto)
    {
        try
        {
            var @event = dto.MapToEntity(id);
            _repository.Update(id, @event);

            return ServiceResponse.Success();

        }
        catch (Exception ex)
        {
            return ServiceResponse.Failure([ex.Message]);
        }
    }

    public ServiceResponse Remove(int id)
    {
        try
        {
            var success = _repository.Remove(id);

            return success
                ? ServiceResponse.Success()
                : ServiceResponse.Failure(["Cannot remove event"]);
        }
        catch (Exception ex)
        {
            return ServiceResponse.Failure([ex.Message]);
        }
    }
}
