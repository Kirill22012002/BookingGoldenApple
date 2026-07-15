using BGA.Domain.Models.Enums;

namespace BGA.Application.Services.Interfaces;

public interface IUserService
{
    Task RegisterAsync(string login, string password, UserRole role = UserRole.User, CancellationToken cancellationToken = default);
    Task<string> LoginAsync(string login, string password, CancellationToken cancellationToken = default);
}
