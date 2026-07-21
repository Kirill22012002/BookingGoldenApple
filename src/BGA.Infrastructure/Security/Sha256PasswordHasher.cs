using System.Security.Cryptography;
using System.Text;
using BGA.Application.Security;

namespace BGA.Infrastructure.Security;

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
