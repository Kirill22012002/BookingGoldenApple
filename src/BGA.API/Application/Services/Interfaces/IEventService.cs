using BGA.API.Presentation.Dtos;

namespace BGA.API.Application.Services.Interfaces;

public interface IEventService
{
    ServiceResponse<List<EventDto>> GetAll();
    ServiceResponse<EventDto> GetById(int id);
    ServiceResponse<EventDto> Create(AddEventDto dto);
    ServiceResponse Change(int id, PutEventDto dto);
    ServiceResponse Remove(int id);
}
