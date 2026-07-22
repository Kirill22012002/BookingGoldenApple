using BGA.Users.Application.Repositories;
using BGA.Users.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BGA.Users.Infrastructure.DataAccess.Repositories;

public sealed class UserRepository(UsersDbContext dbContext) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Users.SingleOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public Task<User?> GetByLoginAsync(string login, CancellationToken cancellationToken = default)
    {
        return dbContext.Users.SingleOrDefaultAsync(user => user.Login == login, cancellationToken);
    }

    public Task<bool> ExistsByLoginAsync(string login, CancellationToken cancellationToken = default)
    {
        return dbContext.Users.AnyAsync(user => user.Login == login, cancellationToken);
    }

    public Task CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        return dbContext.Users.AddAsync(user, cancellationToken).AsTask();
    }
}
