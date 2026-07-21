using System.Net;

namespace BGA.Bookings.Domain.Exceptions;

public sealed class BookingLimitExceededException(
    string message,
    HttpStatusCode statusCode = HttpStatusCode.Conflict) : AppException(message, statusCode)
{
}
