using BGA.Users.Application.Repositories;

namespace BGA.Users.Infrastructure.DataAccess.Repositories;

public sealed class UnitOfWork(
    UsersDbContext dbContext,
    IUserRepository userRepository) : IUnitOfWork
{
    public IUserRepository Users { get; } = userRepository;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
