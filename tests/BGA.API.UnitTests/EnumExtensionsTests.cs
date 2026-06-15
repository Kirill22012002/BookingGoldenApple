using BGA.API.Extensions;
using BGA.Domain.Models.Enums;

namespace BGA.API.UnitTests;

public class EnumExtensionsTests
{
    [Theory]
    [InlineData(BookingStatus.Pending, "pending")]
    [InlineData(BookingStatus.Confirmed, "confirmed")]
    [InlineData(BookingStatus.Rejected, "rejected")]
    public void GetEnumValue_WithEnumMemberAttribute_ReturnsCorrectStringValue(BookingStatus status, string expectedResult)
    {
        // Arrange & Act
        var result = status.GetEnumValue();

        // Arrange
        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData("pending", BookingStatus.Pending)]
    [InlineData("confirmed", BookingStatus.Confirmed)]
    [InlineData("rejected", BookingStatus.Rejected)]
    public void GetEnumFromString_WithEnumMemberAttribute_ReturnsCorrectEnum(string status, BookingStatus expectedResult)
    {
        // Arrange & Act
        var result = status.GetEnumFromString<BookingStatus>();

        // Arrange
        Assert.Equal(expectedResult, result);
    }
}
