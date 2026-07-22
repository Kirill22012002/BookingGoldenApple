using System.Net;

namespace BGA.Events.Domain.Exceptions;

public sealed class NotFoundException(
    string message,
    HttpStatusCode statusCode = HttpStatusCode.NotFound) : AppException(message, statusCode)
{
}
