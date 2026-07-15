using BGA.API.Dtos;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BGA.API.E2ETests.Infrastructure;

public sealed record AuthSession(string Login, string Token);

public static class AuthTestHelper
{
    public static async Task<AuthSession> RegisterAndLoginAsync(HttpClient client, string role = "User", CancellationToken cancellationToken = default)
    {
        var login = $"{role.ToLowerInvariant()}-{Guid.NewGuid():N}";
        const string password = "Password123!";
        var registerResponse = await client.PostAsJsonAsync("/auth/register", new RegisterUserDto
        {
            Login = login,
            Password = password,
            Role = role
        }, cancellationToken);
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await client.PostAsJsonAsync("/auth/login", new LoginUserDto
        {
            Login = login,
            Password = password
        }, cancellationToken);
        loginResponse.EnsureSuccessStatusCode();

        var token = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>(cancellationToken: cancellationToken);
        return new AuthSession(login, token!.Token);
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
