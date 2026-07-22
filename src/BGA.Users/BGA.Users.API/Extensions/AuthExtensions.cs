using BGA.Users.Domain.Models.Enums;

namespace BGA.Users.API.Extensions;

public static class AuthExtensions
{
    public static UserRole ToUserRole(this string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return UserRole.User;
        }

        if (Enum.TryParse<UserRole>(role, true, out var parsedRole))
        {
            return parsedRole;
        }

        return role.ToLowerInvariant().GetEnumFromString<UserRole>();
    }
}
