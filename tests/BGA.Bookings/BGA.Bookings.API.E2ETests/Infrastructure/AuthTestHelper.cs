using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;

namespace BGA.Bookings.API.E2ETests.Infrastructure;

public sealed record AuthSession(Guid UserId, string Token, string Role);

public static class AuthTestHelper
{
    public static AuthSession CreateSession(string role = "User")
    {
        var userId = Guid.NewGuid();
        var token = CreateToken(userId, role);
        return new AuthSession(userId, token, role);
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

    private static string CreateToken(Guid userId, string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes("SuperSecureJwtKeyForBookingGoldenApple2026!")),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "BookingGoldenApple",
            audience: "BookingGoldenApple",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
