using System.Net;

namespace BGA.Users.Domain.Exceptions;

public sealed class NotFoundException(
    string message,
    HttpStatusCode statusCode = HttpStatusCode.NotFound) : AppException(message, statusCode)
{
}
