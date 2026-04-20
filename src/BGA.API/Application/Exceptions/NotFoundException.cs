using System.Net;

namespace BGA.API.Application.Exceptions;

public sealed class NotFoundException(string message, HttpStatusCode statusCode = HttpStatusCode.NotFound) : AppException(message, statusCode)
{
}
