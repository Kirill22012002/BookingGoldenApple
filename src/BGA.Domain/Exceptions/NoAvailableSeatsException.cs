using System.Net;

namespace BGA.Domain.Exceptions;

public sealed class NoAvailableSeatsException(
    string message,
    HttpStatusCode statusCode = HttpStatusCode.Conflict) : AppException(message, statusCode)
{
}
