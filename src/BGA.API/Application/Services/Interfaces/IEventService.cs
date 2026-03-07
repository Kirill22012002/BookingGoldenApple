using BGA.API.Infrastructure.Models;

namespace BGA.API.Application.Services.Interfaces;

public interface IEventService
{
    IEnumerable<Event> GetAll();
    Event GetById(int id);
    Event Create(Event @event);
    void Change(int id, Event @event);
    void Remove(int id);
}
