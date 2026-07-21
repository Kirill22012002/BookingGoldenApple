using BGA.Users.API.Dtos;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BGA.Users.API.E2ETests.Infrastructure;

public sealed record AuthCredentials(string Login, string Password, string Role);
public sealed record AuthSession(string Login, string Token);

public static class AuthTestHelper
{
    public static AuthCredentials CreateCredentials(string role = "User")
    {
        var login = $"{role.ToLowerInvariant()}-{Guid.NewGuid():N}";
        const string password = "Password123!";
        return new AuthCredentials(login, password, role);
    }

    public static Task<HttpResponseMessage> RegisterAsync(HttpClient client, AuthCredentials credentials, CancellationToken cancellationToken = default)
    {
        return client.PostAsJsonAsync("/auth/register", new RegisterUserDto
        {
            Login = credentials.Login,
            Password = credentials.Password,
            Role = credentials.Role
        }, cancellationToken);
    }

    public static Task<HttpResponseMessage> LoginAsync(HttpClient client, AuthCredentials credentials, CancellationToken cancellationToken = default)
    {
        return client.PostAsJsonAsync("/auth/login", new LoginUserDto
        {
            Login = credentials.Login,
            Password = credentials.Password
        }, cancellationToken);
    }

    public static async Task<AuthSession> RegisterAndLoginAsync(HttpClient client, string role = "User", CancellationToken cancellationToken = default)
    {
        var credentials = CreateCredentials(role);
        var registerResponse = await RegisterAsync(client, credentials, cancellationToken);
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await LoginAsync(client, credentials, cancellationToken);
        loginResponse.EnsureSuccessStatusCode();

        var token = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>(cancellationToken: cancellationToken);
        return new AuthSession(credentials.Login, token!.Token);
    }

    public static HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string uri, string token)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    public static HttpRequestMessage CreateAuthorizedRequest<T>(HttpMethod method, string uri, string token, T body)
    {
        var request = CreateAuthorizedRequest(method, uri, token);
        request.Content = JsonContent.Create(body);
        return request;
    }
}
