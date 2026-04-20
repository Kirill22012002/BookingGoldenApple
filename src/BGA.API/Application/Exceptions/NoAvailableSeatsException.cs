using System.Net;

namespace BGA.API.Application.Exceptions;

public sealed class NoAvailableSeatsException(string message, HttpStatusCode statusCode = HttpStatusCode.Conflict) : AppException(message, statusCode)
{
}
