using BGA.Domain.Models;

namespace BGA.Application.Security;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
