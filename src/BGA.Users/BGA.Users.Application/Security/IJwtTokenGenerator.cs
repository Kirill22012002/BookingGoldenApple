using BGA.Users.Domain.Models;

namespace BGA.Users.Application.Security;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
