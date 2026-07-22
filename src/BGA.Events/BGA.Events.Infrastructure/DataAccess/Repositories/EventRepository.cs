using BGA.Events.Application.Repositories;
using BGA.Events.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BGA.Events.Infrastructure.DataAccess.Repositories;

public sealed class EventRepository(EventsDbContext dbContext) : IEventRepository
{
    public IQueryable<Event> GetAll(string? title, DateTimeOffset? from, DateTimeOffset? to)
    {
        var query = dbContext.Events.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(title))
        {
            query = query.Where(@event => EF.Functions.ILike(@event.Title, $"%{title}%"));
        }

        if (from.HasValue)
        {
            query = query.Where(@event => @event.StartAt >= from);
        }

        if (to.HasValue)
        {
            query = query.Where(@event => @event.EndAt <= to);
        }

        return query.OrderBy(@event => @event.Id);
    }

    public Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Events.SingleOrDefaultAsync(@event => @event.Id == id, cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Events.AnyAsync(@event => @event.Id == id, cancellationToken);
    }

    public Task CreateAsync(Event @event, CancellationToken cancellationToken = default)
    {
        return dbContext.Events.AddAsync(@event, cancellationToken).AsTask();
    }

    public void Update(Event @event)
    {
        dbContext.Events.Update(@event);
    }

    public void Remove(Event @event)
    {
        dbContext.Events.Remove(@event);
    }
}
