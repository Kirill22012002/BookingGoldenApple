using BGA.Events.Application.Repositories;

namespace BGA.Events.Infrastructure.DataAccess.Repositories;

public sealed class UnitOfWork(
    EventsDbContext dbContext,
    IEventRepository eventRepository) : IUnitOfWork
{
    public IEventRepository Events { get; } = eventRepository;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
