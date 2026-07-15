using System.Net;

namespace BGA.Domain.Exceptions;

public sealed class EventAlreadyStartedException(
    string message,
    HttpStatusCode statusCode = HttpStatusCode.BadRequest) : AppException(message, statusCode)
{
}
