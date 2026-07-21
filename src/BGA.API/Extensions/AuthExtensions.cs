using BGA.Domain.Models.Enums;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace BGA.API.Extensions;

public static class AuthExtensions
{
    public static Guid GetRequiredUserId(this ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? throw new UnauthorizedAccessException("Authenticated user identifier was not found.");

        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Authenticated user identifier is invalid.");

        return userId;
    }

    public static UserRole ToUserRole(this string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return UserRole.User;

        if (Enum.TryParse<UserRole>(role, true, out var parsedRole))
            return parsedRole;

        return role.ToLowerInvariant().GetEnumFromString<UserRole>();
    }
}
