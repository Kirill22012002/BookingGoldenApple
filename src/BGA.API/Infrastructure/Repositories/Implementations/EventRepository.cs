using BGA.API.Infrastructure.Repositories.Interfaces;
using BGA.API.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace BGA.API.Infrastructure.Repositories.Implementations;

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

    public async Task<bool> CreateAsync(Event @event, CancellationToken cancellationToken = default)
    {
        await _dbContext.Events.AddAsync(@event, cancellationToken);
        var result = await _dbContext.SaveChangesAsync(cancellationToken);
        return result >= 1;
    }

    public async Task<bool> UpdateAsync(Event @event, CancellationToken cancellationToken = default)
    {
        _dbContext.Events.Update(@event);
        var result = await _dbContext.SaveChangesAsync(cancellationToken);
        return result >= 1;
    }

    public async Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            _dbContext.Remove(entity);
            var result = await _dbContext.SaveChangesAsync(cancellationToken);
            return result >= 1;
        }

        return false;
    }
}
