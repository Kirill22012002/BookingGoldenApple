using BGA.API.Infrastructure.Models;

namespace BGA.API.Infrastructure.Repositories.Interfaces;

public interface IEventRepository
{
    IEnumerable<Event> GetAll();
    Event GetById(int id);
    bool Create(Event @event);
    void Update(int id, Event @event);
    bool Remove(int id);
}
