using BGA.API.Infrastructure.Models;

namespace BGA.API.Infrastructure.Repositories.Interfaces;

public interface IEventRepository
{
    IQueryable<Event> GetAll();
    Event GetById(Guid id);
    bool Create(Event @event);
    bool Update(Guid id, Event @event);
    bool Remove(Guid id);
}
