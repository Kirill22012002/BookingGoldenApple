using BGA.API.Presentation.Dtos;

namespace BGA.API.Application.Services.Interfaces;

public interface IEventService
{
    List<EventDto> GetAll();
    EventDto GetById(int id);
    void Create(AddEventDto dto);
    void Change(int id, PutEventDto dto);
    void Remove(int id);
}
