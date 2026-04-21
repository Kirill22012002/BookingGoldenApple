using BGA.API.Infrastructure.Models;
using BGA.API.Tests.Helpers;

namespace BGA.API.Tests;

public class EventTests
{
    [Theory]
    [InlineData(1, 1, 0, true)]
    [InlineData(2, 1, 1, true)]
    [InlineData(2, 2, 0, true)]
    [InlineData(5, 0, 5, true)]
    [InlineData(1, 2, 1, false)]
    public void TryReserveSeats(int initialTotalSeats, int seatsCountToReserve, int availableAfterReserve, bool expectedResult)
    {
        // Arrange
        var @event = new Event("title", "description", TestHelper.Yesterday, TestHelper.Tomorrow, initialTotalSeats);

        // Act & Assert
        Assert.Equal(initialTotalSeats, @event.TotalSeats);
        Assert.Equal(initialTotalSeats, @event.AvailableSeats);

        var result = @event.TryReserveSeats(seatsCountToReserve);
        Assert.Equal(expectedResult, result);
        Assert.Equal(initialTotalSeats, @event.TotalSeats);
        Assert.Equal(availableAfterReserve, @event.AvailableSeats);
    }

    [Theory]
    [InlineData(1, 1, 1)]
    public void ReleaseSeats(int initialTotalSeats, int seatsCountToRelease, int availableAfterRelease)
    {
        // Arrange
        var @event = new Event("title", "description", TestHelper.Yesterday, TestHelper.Tomorrow, initialTotalSeats);

        // Act & Assert
        Assert.Equal(initialTotalSeats, @event.TotalSeats);
        Assert.Equal(initialTotalSeats, @event.AvailableSeats);

        @event.ReleaseSeats(seatsCountToRelease);
        Assert.Equal(initialTotalSeats, @event.TotalSeats);
        Assert.Equal(availableAfterRelease, @event.AvailableSeats);
    }

    [Theory]
    [InlineData(1, 1, 0, true, 1, 1)]
    [InlineData(1, 1, 0, true, 2, 1)]
    [InlineData(5, 2, 3, true, 1, 4)]
    [InlineData(5, 5, 0, true, 5, 5)]
    [InlineData(5, 5, 0, true, 2, 2)]
    [InlineData(5, 0, 5, true, 2, 5)]
    [InlineData(5, 3, 2, true, 0, 2)]
    [InlineData(5, 0, 5, true, 0, 5)]
    [InlineData(5, 3, 2, true, 4, 5)]
    [InlineData(5, 2, 3, true, 3, 5)]
    [InlineData(2, 3, 2, false, 1, 2)]
    [InlineData(2, 3, 2, false, 5, 2)]
    public void TryReserveSeats_Then_ReleaseSeats(int initialTotalSeats, int seatsCountToReserve, int availableAfterReserve, bool expectedResult, int seatsCountToRelease, int availableAfterRelease)
    {
        // Arrange
        var @event = new Event("title", "description", TestHelper.Yesterday, TestHelper.Tomorrow, initialTotalSeats);

        // Act & Assert
        Assert.Equal(initialTotalSeats, @event.TotalSeats);
        Assert.Equal(initialTotalSeats, @event.AvailableSeats);

        var result = @event.TryReserveSeats(seatsCountToReserve);
        Assert.Equal(expectedResult, result);
        Assert.Equal(availableAfterReserve, @event.AvailableSeats);

        @event.ReleaseSeats(seatsCountToRelease);

        Assert.Equal(initialTotalSeats, @event.TotalSeats);
        Assert.Equal(availableAfterRelease, @event.AvailableSeats);
    }
}
