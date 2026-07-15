using System.Net;

namespace BGA.Domain.Exceptions;

public sealed class NotFoundException(
    string message,
    HttpStatusCode statusCode = HttpStatusCode.NotFound) : AppException(message, statusCode)
{
}
