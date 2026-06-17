using BGA.Application.Repositories;
using BGA.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BGA.Infrastructure.DataAccess.Repositories;

public class EventRepository(ApplicationDbContext _dbContext) : IEventRepository
{
    public IQueryable<Event> GetAll(string? title, DateTimeOffset? from, DateTimeOffset? to)
    {
        var query = _dbContext.Events.AsNoTracking();

        if (!string.IsNullOrEmpty(title))
            query = query.Where(@event => EF.Functions.ILike(@event.Title, $"%{title}%"));
        if (from.HasValue)
            query = query.Where(@event => @event.StartAt >= from);
        if (to.HasValue)
            query = query.Where(@event => @event.EndAt <= to);

        query = query.OrderBy(@event => @event.Id);

        return query;
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
