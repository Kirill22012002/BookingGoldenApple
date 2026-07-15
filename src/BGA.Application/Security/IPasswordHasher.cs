namespace BGA.Application.Security;

public interface IPasswordHasher
{
    string Hash(string value);
    bool Verify(string value, string hash);
}
