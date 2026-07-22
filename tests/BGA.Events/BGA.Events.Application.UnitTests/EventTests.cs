using BGA.Events.Application.UnitTests.Helpers;
using BGA.Events.Domain.Exceptions;
using BGA.Events.Domain.Models;

namespace BGA.Events.Application.UnitTests;

public class EventTests
{
    [Fact]
    public void CreateEvent_WithValidAllProperties_ShouldCreateCorrectEvent()
    {
        // Arrange
        var expectedTitle = "title";
        var expectedDescription = "description";
        var expectedStartAt = TestHelper.Yesterday;
        var expectedEndAt = TestHelper.Tomorrow;
        var expectedTotalSeats = 1;
        var expectedAvailableSeats = expectedTotalSeats;

        // Act
        var @event = new Event(expectedTitle, expectedDescription, expectedStartAt, expectedEndAt, expectedTotalSeats);

        // Assert
        Assert.NotNull(@event);
        Assert.Equal(expectedTitle, @event.Title);
        Assert.Equal(expectedDescription, @event.Description);
        Assert.Equal(expectedStartAt, @event.StartAt);
        Assert.Equal(expectedEndAt, @event.EndAt);
        Assert.Equal(expectedTotalSeats, @event.TotalSeats);
        Assert.Equal(expectedAvailableSeats, @event.AvailableSeats);
    }

    [Fact]
    public void CreateEvent_WithNotValidStartAt_ThrowValidationException()
    {
        // Arrange & Act
        var exception = Assert.Throws<ValidationException>(
            () => new Event("title", "description", DateTimeOffset.MinValue, TestHelper.Tomorrow, 1));

        // Assert
        exception.HasSingleError("startAt", "startAt must be valid value (not default value)");
    }

    [Fact]
    public void CreateEvent_WithNotValidEndAt_ThrowValidationException()
    {
        // Arrange & Act
        var exception = Assert.Throws<ValidationException>(
            () => new Event("title", "description", TestHelper.Yesterday, DateTimeOffset.MinValue, 1));

        // Assert
        exception.HasSingleError("endAt", "endAt must be valid value (not default value)");
    }

    [Fact]
    public void CreateEvent_WithEndAtEqualThanStartAt_ThrowValidationException()
    {
        // Arrange & Act
        var date = TestHelper.Now;
        var exception = Assert.Throws<ValidationException>(
            () => new Event("title", "description", date, date, 1));

        // Assert
        exception.HasSingleError("endAt", "endAt must be greater than the startAt");
    }

    [Fact]
    public void CreateEvent_WithEndAtLessThanStartAt_ThrowValidationException()
    {
        // Arrange & Act
        var exception = Assert.Throws<ValidationException>(
            () => new Event("title", "description", TestHelper.Tomorrow, TestHelper.Yesterday, 1));

        // Assert
        exception.HasSingleError("endAt", "endAt must be greater than the startAt");
    }

    [Fact]
    public void CreateEvent_WithTotalSeatsEqualThanZero_ThrowValidationException()
    {
        // Arrange & Act
        var exception = Assert.Throws<ValidationException>(
            () => new Event("title", "description", TestHelper.Yesterday, TestHelper.Tomorrow, 0));

        // Assert
        exception.HasSingleError("totalSeats", "totalSeats must be valid value (not default value)");
    }

    [Fact]
    public void CreateEvent_WithTotalSeatsLessThanZero_ThrowValidationException()
    {
        // Arrange & Act
        var exception = Assert.Throws<ValidationException>(
            () => new Event("title", "description", TestHelper.Yesterday, TestHelper.Tomorrow, -1));

        // Assert
        exception.HasSingleError("totalSeats", "totalSeats must be valid value (not default value)");
    }

    [Fact]
    public void Reschedule_WithValidStartAtAndEndAt_ShouldUpdateStartAtAndEndAt()
    {
        var initialStartAt = TestHelper.Yesterday;
        var initialEndAt = TestHelper.Tomorrow;
        var @event = new Event("title", "description", initialStartAt, initialEndAt, 1);
        var newStartAt = TestHelper.Tomorrow;
        var newEndAt = TestHelper.Tomorrow.AddDays(1);

        @event.Reschedule(newStartAt, newEndAt);

        Assert.Equal(newStartAt, @event.StartAt);
        Assert.Equal(newEndAt, @event.EndAt);
    }

    [Fact]
    public void Reschedule_WithNotValidStartAtAndEndAt_ThrowValidationException()
    {
        var initialStartAt = TestHelper.Yesterday;
        var initialEndAt = TestHelper.Tomorrow;
        var @event = new Event("title", "description", initialStartAt, initialEndAt, 1);

        var exception = Assert.Throws<ValidationException>(
            () => @event.Reschedule(TestHelper.Tomorrow, TestHelper.Yesterday));

        exception.HasSingleError("endAt", "endAt must be greater than the startAt");
        Assert.Equal(initialStartAt, @event.StartAt);
        Assert.Equal(initialEndAt, @event.EndAt);
    }

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

    [Fact]
    public void ReleaseSeats()
    {
        // Arrange
        var initialTotalSeats = 1;
        var seatsCountToRelease = 1;
        var availableAfterRelease = 1;
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

