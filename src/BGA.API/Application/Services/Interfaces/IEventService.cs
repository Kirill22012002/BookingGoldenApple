using BGA.API.Infrastructure.Models;

namespace BGA.API.Application.Services.Interfaces;

public interface IEventService
{
    ServiceResponse<PaginatedResult<Event>> GetAll(string? title, DateTime? from, DateTime? to, int page, int pageSize);
    ServiceResponse<Event> GetById(Guid id);
    ServiceResponse<Event> Create(Event @event);
    ServiceResponse Update(Guid id, Event @event);
    ServiceResponse Remove(Guid id);
}
