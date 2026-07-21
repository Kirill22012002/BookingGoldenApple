using System.Net;

namespace BGA.Bookings.Domain.Exceptions;

public sealed class OperationForbiddenException(
    string message,
    HttpStatusCode statusCode = HttpStatusCode.Forbidden) : AppException(message, statusCode)
{
}
