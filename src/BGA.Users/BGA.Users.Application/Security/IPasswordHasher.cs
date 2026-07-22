namespace BGA.Users.Application.Security;

public interface IPasswordHasher
{
    string Hash(string value);
    bool Verify(string value, string hash);
}
