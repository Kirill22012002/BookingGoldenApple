namespace BGA.Events.Application.Repositories;

public interface IUnitOfWork
{
    IEventRepository Events { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
