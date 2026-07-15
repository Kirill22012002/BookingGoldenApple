using BGA.API.Extensions;
using BGA.Domain.Models.Enums;

namespace BGA.API.UnitTests;

public class EnumExtensionsTests
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

    [Theory]
    [InlineData("pending", BookingStatus.Pending)]
    [InlineData("confirmed", BookingStatus.Confirmed)]
    [InlineData("rejected", BookingStatus.Rejected)]
    [InlineData("cancelled", BookingStatus.Cancelled)]
    public void GetEnumFromString_WithEnumMemberAttribute_ReturnsCorrectEnum(string status, BookingStatus expectedResult)
    {
        var result = status.GetEnumFromString<BookingStatus>();

        Assert.Equal(expectedResult, result);
    }
}
