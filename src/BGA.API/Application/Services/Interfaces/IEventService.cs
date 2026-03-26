using BGA.API.Presentation.Dtos;

namespace BGA.API.Application.Services.Interfaces;

public interface IEventService
{
    ServiceResponse<PaginatedResult<EventDto>> GetAll(string? title, DateTime? from, DateTime? to, int page, int pageSize);
    ServiceResponse<EventDto> GetById(int id);
    ServiceResponse<EventDto> Create(AddEventDto dto);
    ServiceResponse Update(int id, PutEventDto dto);
    ServiceResponse Remove(int id);
}
