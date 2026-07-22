using BGA.Bookings.API.Extensions;
using BGA.Bookings.Domain.Models.Enums;

namespace BGA.Bookings.API.UnitTests;

public class BookingStatusExtensionsTests
{
    [Theory]
    [InlineData(BookingStatus.Pending, "pending")]
    [InlineData(BookingStatus.Confirmed, "confirmed")]
    [InlineData(BookingStatus.Rejected, "rejected")]
    [InlineData(BookingStatus.Cancelled, "cancelled")]
    public void GetEnumValue_WithEnumMemberAttribute_ReturnsCorrectStringValue(BookingStatus status, string expectedResult)
    {
        var result = status.GetEnumValue();

        Assert.Equal(expectedResult, result);
    }

}

