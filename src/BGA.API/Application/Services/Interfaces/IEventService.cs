using BGA.API.Infrastructure.Models;

namespace BGA.API.Application.Services.Interfaces;

public interface IEventService
{
    ServiceResult<IEnumerable<Event>> GetAll();
    ServiceBaseResult GetById(int id);
    ServiceResult<Event> Create(Event @event);
    ServiceResult Change(int id, Event @event);
    ServiceResult Remove(int id);
}
