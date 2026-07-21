using BGA.Users.API.Dtos;
using BGA.Users.API.E2ETests.Infrastructure;
using BGA.Users.Domain.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;

namespace BGA.Users.API.E2ETests;

public class AuthApiTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.HttpClient;
    private readonly CustomWebApplicationFactory _factory = factory;

    [Fact]
    public async Task POST_Register_ShouldCreateUserAndReturnNoContent()
    {
        await _factory.ResetDatabaseAsync();

        var credentials = AuthTestHelper.CreateCredentials();
        var response = await AuthTestHelper.RegisterAsync(_client, credentials, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await using var verifyContext = _factory.CreateContext();
        var savedUser = await verifyContext.Users.AsNoTracking()
            .SingleAsync(user => user.Login == credentials.Login, TestContext.Current.CancellationToken);

        Assert.Equal(UserRole.User, savedUser.Role);
        Assert.NotEqual(credentials.Password, savedUser.PasswordHash);
        Assert.Equal(64, savedUser.PasswordHash.Length);
    }

    [Fact]
    public async Task POST_Register_WhenLoginAlreadyExists_ShouldReturnBadRequest()
    {
        await _factory.ResetDatabaseAsync();

        var credentials = AuthTestHelper.CreateCredentials();
        await AuthTestHelper.RegisterAsync(_client, credentials, TestContext.Current.CancellationToken);

        var response = await AuthTestHelper.RegisterAsync(_client, credentials, TestContext.Current.CancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("already exists", responseText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task POST_Login_ShouldReturnToken()
    {
        await _factory.ResetDatabaseAsync();

        var credentials = AuthTestHelper.CreateCredentials("Admin");
        await AuthTestHelper.RegisterAsync(_client, credentials, TestContext.Current.CancellationToken);

        var response = await AuthTestHelper.LoginAsync(_client, credentials, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.NotEmpty(body.Token);
        Assert.Equal(3, body.Token.Split('.').Length);
    }

    [Fact]
    public async Task POST_Login_WhenCredentialsAreInvalid_ShouldReturnNotFound()
    {
        await _factory.ResetDatabaseAsync();

        var credentials = AuthTestHelper.CreateCredentials();
        await AuthTestHelper.RegisterAsync(_client, credentials, TestContext.Current.CancellationToken);

        var invalidCredentials = credentials with { Password = "WrongPassword123!" };
        var response = await AuthTestHelper.LoginAsync(_client, invalidCredentials, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var responseText = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("Invalid credentials", responseText, StringComparison.OrdinalIgnoreCase);
    }
}
