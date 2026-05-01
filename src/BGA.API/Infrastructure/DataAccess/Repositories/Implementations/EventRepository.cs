using BGA.API.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using BGA.API.Infrastructure.DataAccess.Repositories.Interfaces;

namespace BGA.API.Infrastructure.DataAccess.Repositories.Implementations;

public class EventRepository(ApplicationDbContext _dbContext) : IEventRepository
{
    public async Task<IQueryable<Event>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _dbContext.Events.AsNoTracking().AsQueryable();
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
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Event @event, CancellationToken cancellationToken = default)
    {
        _dbContext.Events.Update(@event);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Event @event, CancellationToken cancellationToken = default)
    {
        _dbContext.Remove(@event);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
