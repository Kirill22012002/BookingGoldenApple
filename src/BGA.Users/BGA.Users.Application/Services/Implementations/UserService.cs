using BGA.Users.Application.Repositories;
using BGA.Users.Application.Security;
using BGA.Users.Application.Services.Interfaces;
using BGA.Users.Domain.Exceptions;
using BGA.Users.Domain.Models;
using BGA.Users.Domain.Models.Enums;

namespace BGA.Users.Application.Services.Implementations;

public sealed class UserService(
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator) : IUserService
{
    public async Task RegisterAsync(string login, string password, UserRole role = UserRole.User, CancellationToken cancellationToken = default)
    {
        ValidateCredentials(login, password);

        if (await unitOfWork.Users.ExistsByLoginAsync(login, cancellationToken))
        {
            throw new ValidationException(nameof(login), "User with this login already exists.");
        }

        var user = new User(login, passwordHasher.Hash(password), role);
        await unitOfWork.Users.CreateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<string> LoginAsync(string login, string password, CancellationToken cancellationToken = default)
    {
        ValidateCredentials(login, password);

        var user = await unitOfWork.Users.GetByLoginAsync(login, cancellationToken);
        if (user is null || !passwordHasher.Verify(password, user.PasswordHash))
        {
            throw new NotFoundException("Invalid credentials");
        }

        return jwtTokenGenerator.GenerateToken(user);
    }

    private static void ValidateCredentials(string login, string password)
    {
        if (string.IsNullOrWhiteSpace(login))
        {
            throw new ValidationException(nameof(login), "login must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ValidationException(nameof(password), "password must not be empty.");
        }
    }
}
