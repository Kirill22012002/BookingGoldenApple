using BGA.Domain.Exceptions;
using BGA.Domain.Models;
using BGA.Domain.Models.Enums;
using BGA.Infrastructure.DataAccess.Repositories;
using BGA.Infrastructure.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BGA.Infrastructure.IntegrationTests;

public class UserRepositoryTests : PostgresInfrastructure
{
    [Fact]
    public async Task GetByLoginAsync_WhenUserExists_ReturnsUser()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var user = CreateUser();
        await context.Users.AddAsync(user, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new UserRepository(CreateContext());
        var result = await repository.GetByLoginAsync(user.Login, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal(user.Role, result.Role);
    }

    [Fact]
    public async Task ExistsByLoginAsync_WhenUserExists_ReturnsTrue()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var user = CreateUser();
        await context.Users.AddAsync(user, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new UserRepository(CreateContext());
        var result = await repository.ExistsByLoginAsync(user.Login, TestContext.Current.CancellationToken);

        Assert.True(result);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenLoginAlreadyExists_ThrowsValidationException()
    {
        await ResetDatabaseAsync();

        var login = $"user-{Guid.NewGuid():N}";

        await using (var firstContext = CreateContext())
        {
            var firstUnitOfWork = CreateUnitOfWork(firstContext);
            await firstUnitOfWork.Users.CreateAsync(new User(login, new string('A', 64), UserRole.User), TestContext.Current.CancellationToken);
            await firstUnitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var secondContext = CreateContext();
        var secondUnitOfWork = CreateUnitOfWork(secondContext);
        await secondUnitOfWork.Users.CreateAsync(new User(login, new string('B', 64), UserRole.Admin), TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            secondUnitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken));

        Assert.Contains("already exists", string.Join(' ', exception.Errors.SelectMany(error => error.Value)), StringComparison.OrdinalIgnoreCase);
    }

    private static UnitOfWork CreateUnitOfWork(DbContext context)
    {
        var applicationDbContext = (BGA.Infrastructure.DataAccess.ApplicationDbContext)context;
        return new UnitOfWork(
            applicationDbContext,
            new EventRepository(applicationDbContext),
            new BookingRepository(applicationDbContext),
            new UserRepository(applicationDbContext));
    }

    private static User CreateUser()
        => new($"user-{Guid.NewGuid():N}", new string('A', 64), UserRole.User);
}
