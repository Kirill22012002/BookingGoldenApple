using BGA.API.Infrastructure.Models;

namespace BGA.API.Infrastructure.Repositories;

public class EventRepository : List<Event>
{
    private int _eventId = 0;

    public new void Add(Event item)
    {
        if (item.Id <= 0)
        {
            Interlocked.Increment(ref _eventId);
            item.Id = _eventId;
        }

        base.Add(item);
    }
}
