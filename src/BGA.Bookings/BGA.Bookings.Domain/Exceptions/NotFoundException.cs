using System.Net;

namespace BGA.Bookings.Domain.Exceptions;

public sealed class NotFoundException(
    string message,
    HttpStatusCode statusCode = HttpStatusCode.NotFound) : AppException(message, statusCode)
{
}
