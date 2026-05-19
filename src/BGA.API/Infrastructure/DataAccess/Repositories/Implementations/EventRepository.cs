using BGA.API.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using BGA.API.Infrastructure.DataAccess.Repositories.Interfaces;

namespace BGA.API.Infrastructure.DataAccess.Repositories.Implementations;

public class EventRepository(ApplicationDbContext _dbContext) : IEventRepository
{
    public IQueryable<Event> GetAll()
    {
        return _dbContext.Events.AsNoTracking();
    }

    public async Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Events.SingleOrDefaultAsync(@event => @event.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var @event = await GetByIdAsync(id, cancellationToken);
        return @event != null;
    }

    public async Task CreateAsync(Event @event, CancellationToken cancellationToken = default)
    {
        await _dbContext.Events.AddAsync(@event, cancellationToken);
    }

    public void Update(Event @event)
    {
        _dbContext.Events.Update(@event);
    }

    public void Remove(Event @event)
    {
        _dbContext.Events.Remove(@event);
    }
}
