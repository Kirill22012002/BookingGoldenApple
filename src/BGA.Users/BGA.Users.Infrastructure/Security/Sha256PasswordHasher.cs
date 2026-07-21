using BGA.Users.Application.Security;
using System.Security.Cryptography;
using System.Text;

namespace BGA.Users.Infrastructure.Security;

public sealed class Sha256PasswordHasher : IPasswordHasher
{
    public string Hash(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }

    public bool Verify(string value, string hash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);

        var actualHashBytes = Encoding.UTF8.GetBytes(Hash(value));
        var expectedHashBytes = Encoding.UTF8.GetBytes(hash.Trim().ToUpperInvariant());

        return CryptographicOperations.FixedTimeEquals(actualHashBytes, expectedHashBytes);
    }
}
