using BGA.Domain.Models;
using BGA.Domain.Models.Enums;
using BGA.Infrastructure.DataAccess.Repositories;
using BGA.Infrastructure.IntegrationTests.Infrastructure;

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

    private static User CreateUser()
        => new($"user-{Guid.NewGuid():N}", new string('A', 64), UserRole.User);
}
