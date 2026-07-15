using BGA.Domain.Exceptions;
using BGA.Domain.Models.Enums;

namespace BGA.Domain.Models;

public class User
{
    public Guid Id { get; private set; }
    public string Login { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public List<Booking> Bookings { get; private set; } = [];

    private User() { }

    public User(string login, string passwordHash, UserRole role)
    {
        ValidateRequired(login, nameof(login));
        ValidateRequired(passwordHash, nameof(passwordHash));

        Id = Guid.NewGuid();
        Login = login;
        PasswordHash = passwordHash;
        Role = role;
    }

    private static void ValidateRequired(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ValidationException(fieldName, $"{fieldName} must not be empty.");
    }
}
