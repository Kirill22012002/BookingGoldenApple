using BGA.Bookings.Domain.Exceptions;

namespace BGA.Bookings.Application.UnitTests.Helpers;

public static class ValidationExceptionAssertions
{
    public static void HasSingleError(this ValidationException exception, string field, string error)
    {
        var singleValidationError = Assert.Single(exception.Errors);
        Assert.Equal(field, singleValidationError.Key);
        Assert.Contains(error, singleValidationError.Value);
    }
}

