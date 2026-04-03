using BGA.API.Infrastructure.Models;

namespace BGA.API.Infrastructure.Repositories.Interfaces;

public interface IEventRepository
{
    IQueryable<Event> GetAll();
    Event GetById(int id);
    bool Create(Event @event);
    bool Update(int id, Event @event);
    bool Remove(int id);
}
