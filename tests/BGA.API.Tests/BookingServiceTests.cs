using BGA.API.Application.Exceptions;
using BGA.API.Application.Services.Implementations;
using BGA.API.Infrastructure.DataAccess;
using BGA.API.Infrastructure.DataAccess.Repositories.Interfaces;
using BGA.API.Infrastructure.Models;
using BGA.API.Infrastructure.Models.Enums;
using BGA.API.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace BGA.API.Tests;

public class BookingServiceTests
{
    private readonly Mock<IEventRepository> _eventRepositoryMock;
    private readonly Mock<IBookingRepository> _bookingRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ILogger<BookingService>> _logger;
    private readonly FakeTimeProvider _timeProvider;
    private readonly BookingService _service;

    public BookingServiceTests()
    {
        _eventRepositoryMock = new Mock<IEventRepository>();
        _bookingRepositoryMock = new Mock<IBookingRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _unitOfWork.Setup(u => u.Events).Returns(_eventRepositoryMock.Object);
        _unitOfWork.Setup(u => u.Bookings).Returns(_bookingRepositoryMock.Object);
        _logger = new Mock<ILogger<BookingService>>();
        _timeProvider = new FakeTimeProvider();
        _service = new BookingService(
            _unitOfWork.Object,
            _logger.Object,
            _timeProvider);
    }

    [Fact]
    public async Task CreateBookingAsync_WithExistsEvent_ReturnsBooking()
    {
        // Arrange
        var expectedBookingStatus = BookingStatus.Pending;
        var @event = new Event("title", "description", TestHelper.Yesterday, TestHelper.Tomorrow, int.MaxValue);

        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Events.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(@event);

        // Act
        var result = await _service.CreateBookingAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedBookingStatus, result.Status);
        Assert.Equal(_timeProvider.GetUtcNow(), result.CreatedAt);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.Update(It.IsAny<Event>()), Times.Once);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Bookings.CreateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsync_WithExistsEvent_DecrementAvailableSeats_And_ReturnsBooking()
    {
        // Arrange
        var initialTotalSeats = 5;
        var expectedAvailableSeats = initialTotalSeats - 1;
        var @event = new Event("title", "description", TestHelper.Yesterday, TestHelper.Tomorrow, initialTotalSeats);

        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Events.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(@event);

        // Act
        var result = await _service.CreateBookingAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedAvailableSeats, @event.AvailableSeats);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.Update(It.IsAny<Event>()), Times.Once);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Bookings.CreateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsync_WithExistsEvent_CreateBeforeLimit_AllBookingsWithUniqueId_ReturnsBooking()
    {
        // Arrange
        var initialTotalSeats = 5;
        var @event = new Event("title", "description", TestHelper.Yesterday, TestHelper.Tomorrow, initialTotalSeats);
        List<Guid> createdBookingIds = [];

        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Events.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(@event);

        // Act & Assert
        for (var i = 0; i < initialTotalSeats; i++)
        {
            _unitOfWork
                .Setup(unitOfWork => unitOfWork.Bookings.CreateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken));

            var result = await _service.CreateBookingAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken);
            Assert.NotNull(result);
            Assert.DoesNotContain(result.Id, createdBookingIds);

            createdBookingIds.Add(result.Id);
        }

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken), Times.Exactly(initialTotalSeats));

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.Update(It.IsAny<Event>()), Times.Exactly(initialTotalSeats));

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Bookings.CreateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken), Times.Exactly(initialTotalSeats));

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Exactly(initialTotalSeats));
    }

    [Fact]
    public async Task CreateBookingAsync_TwiceWithTheSameEventIdAndExistsEvent_ReturnsDifferentBookingIds()
    {
        // Arrange
        var @event = new Event("title", "description", TestHelper.Yesterday, TestHelper.Tomorrow, int.MaxValue);
        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Events.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(@event);

        var capturedEntities = new List<Booking>();

        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Bookings.CreateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken))
            .Callback<Booking, CancellationToken>((booking, cancellationToken) =>
            {
                capturedEntities.Add(booking);
            });

        // Act
        var firstBookingResult = await _service.CreateBookingAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken);
        var secondBookingResult = await _service.CreateBookingAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(firstBookingResult);
        Assert.NotNull(secondBookingResult);
        Assert.Equal(firstBookingResult.EventId, secondBookingResult.EventId);
        Assert.NotEqual(firstBookingResult.Id, secondBookingResult.Id);
        Assert.NotSame(capturedEntities[0], capturedEntities[1]);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.Update(It.IsAny<Event>()), Times.Exactly(2));

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken), Times.Exactly(2));

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Bookings.CreateAsync(capturedEntities[0], cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Bookings.CreateAsync(capturedEntities[1], cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Exactly(2));
    }

    [Fact]
    public async Task CreateBookingAsync_WhenEventRemoved_ThrowNotFoundException()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Events.GetByIdAsync(eventId, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync((Event)null!);

        // Act
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            async () => await _service.CreateBookingAsync(eventId, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal("Event not found", exception.Message);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.GetByIdAsync(eventId, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.Update(It.IsAny<Event>()), Times.Never);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Bookings.CreateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken), Times.Never);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Never);
    }

    [Fact]
    public async Task CreateBookingAsync_WithRepositoryThrowsException()
    {
        // Arrange
        var @event = new Event("title", "description", TestHelper.Yesterday, TestHelper.Tomorrow, int.MaxValue);

        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Events.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(@event);

        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Bookings.CreateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken))
            .ThrowsAsync(new Exception());

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            async () => await _service.CreateBookingAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken));

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.Update(It.IsAny<Event>()), Times.Once);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Bookings.CreateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Never);
    }

    [Fact]
    public async Task CreateBookingAsync_AfterLastAvailableSeat_NoAvailableSeats_ThrowNoAvailableSeatsException()
    {
        // Arrange
        var availableSeats = 1;
        var @event = new Event("title", "description", TestHelper.Yesterday, TestHelper.Tomorrow, availableSeats);

        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Events.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(@event);

        await _service.CreateBookingAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<NoAvailableSeatsException>(
            async () => await _service.CreateBookingAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal("No available seats for this event", exception.Message);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken), Times.Exactly(2));

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.Update(It.IsAny<Event>()), Times.Once);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Bookings.CreateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetBookingByIdAsync_WithExistingBooking_ReturnsBooking()
    {
        // Arrange
        var booking = new Booking(Guid.NewGuid(), BookingStatus.Pending, _timeProvider.GetUtcNow());

        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Bookings.GetByIdAsync(booking.Id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(booking);

        // Act
        var result = await _service.GetBookingByIdAsync(booking.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(booking.Id, result.Id);
        Assert.Equal(booking.EventId, result.EventId);
        Assert.Equal(booking.Status, result.Status);
        Assert.Equal(booking.CreatedAt, result.CreatedAt);
        Assert.Equal(booking.ProcessedAt, result.ProcessedAt);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Bookings.GetByIdAsync(booking.Id, cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetBookingByIdAsync_WithExistingBooking_ReturnsBookingWithConfirmedStatus()
    {
        // Arrange
        var booking = new Booking(Guid.NewGuid(), BookingStatus.Confirmed, _timeProvider.GetUtcNow());

        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Bookings.GetByIdAsync(booking.Id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(booking);

        // Act
        var result = await _service.GetBookingByIdAsync(booking.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(booking.Status, result.Status);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Bookings.GetByIdAsync(booking.Id, cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetBookingByIdAsync_WithExistingBooking_ReturnsBookingWithRejectedStatus()
    {
        // Arrange
        var booking = new Booking(Guid.NewGuid(), BookingStatus.Rejected, _timeProvider.GetUtcNow());

        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Bookings.GetByIdAsync(booking.Id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(booking);

        // Act
        var result = await _service.GetBookingByIdAsync(booking.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(booking.Status, result.Status);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Bookings.GetByIdAsync(booking.Id, cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetBookingByIdAsync_WithNotExistingBooking_ThrowNotFoundException()
    {
        // Arrange
        var bookingId = Guid.NewGuid();

        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Bookings.GetByIdAsync(bookingId, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync((Booking)null!);

        // Act
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            async () => await _service.GetBookingByIdAsync(bookingId, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal("Booking not found", exception.Message);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Bookings.GetByIdAsync(bookingId, cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetBookingByIdAsync_WithRepositoryThrowsException()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Bookings.GetByIdAsync(bookingId, cancellationToken: TestContext.Current.CancellationToken))
            .ThrowsAsync(new Exception());

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            async () => await _service.GetBookingByIdAsync(bookingId, cancellationToken: TestContext.Current.CancellationToken));

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Bookings.GetByIdAsync(bookingId, cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task ProcessBookingAsync_WithExistsEvent_BookingConfirmed()
    {
        // Arrange
        var expectedBookingStatus = BookingStatus.Confirmed;
        var booking = new Booking(Guid.NewGuid(), BookingStatus.Pending, TestHelper.Yesterday);

        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Events.ExistsAsync(booking.EventId, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(true);

        // Act
        await _service.ProcessBookingAsync(booking, cancellationToken: TestContext.Current.CancellationToken);

        // Arrange
        Assert.Equal(expectedBookingStatus, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
        Assert.NotEqual(default(DateTimeOffset), booking.ProcessedAt);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.ExistsAsync(booking.EventId, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.GetByIdAsync(booking.EventId, cancellationToken: TestContext.Current.CancellationToken), Times.Never);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.Update(It.IsAny<Event>()), Times.Never);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Bookings.Update(It.IsAny<Booking>()), Times.Once);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task ProcessBookingAsync_WithNotExistsEvent_BookingRejected()
    {
        // Arrange
        var expectedBookingStatus = BookingStatus.Rejected;
        var booking = new Booking(Guid.NewGuid(), BookingStatus.Pending, TestHelper.Yesterday);

        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Events.ExistsAsync(booking.EventId, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(false);

        // Act
        await _service.ProcessBookingAsync(booking, cancellationToken: TestContext.Current.CancellationToken);

        // Arrange
        Assert.Equal(expectedBookingStatus, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
        Assert.NotEqual(default(DateTimeOffset), booking.ProcessedAt);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.ExistsAsync(booking.EventId, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.GetByIdAsync(booking.EventId, cancellationToken: TestContext.Current.CancellationToken), Times.Never);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.Update(It.IsAny<Event>()), Times.Never);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Bookings.Update(It.IsAny<Booking>()), Times.Once);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task ProcessBookingAsync_WhenEventRepositoryThrowException_BookingRejectedAndEventReleasedSeats_ThrowsException()
    {
        // Arrange
        var initialSeats = 2;
        var expectedException = new InvalidOperationException();
        var expectedBookingStatus = BookingStatus.Rejected;
        var @event = new Event("title", "description", TestHelper.Yesterday, TestHelper.Tomorrow, initialSeats);
        var booking = new Booking(Guid.NewGuid(), BookingStatus.Pending, TestHelper.Yesterday);

        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Events.ExistsAsync(booking.EventId, cancellationToken: TestContext.Current.CancellationToken))
            .Throws(expectedException);

        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Events.GetByIdAsync(booking.EventId, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(@event);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.ProcessBookingAsync(booking, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(expectedBookingStatus, booking.Status);
        Assert.Equal(initialSeats, @event.AvailableSeats);
        Assert.NotNull(booking.ProcessedAt);
        Assert.NotEqual(default(DateTimeOffset), booking.ProcessedAt);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.ExistsAsync(booking.EventId, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.GetByIdAsync(booking.EventId, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.Update(It.IsAny<Event>()), Times.Once);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Bookings.Update(It.IsAny<Booking>()), Times.Once);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task ProcessBookingAsync_WhenEventRepositoryThrowException_BookingRejectedButEventNotExistsSoWithoutReleasedSeats_ThrowException()
    {
        // Arrange
        var initialSeats = 2;
        var expectedMainException = new InvalidOperationException();
        var innerException = new Exception();
        var expectedBookingStatus = BookingStatus.Rejected;
        var @event = new Event("title", "description", TestHelper.Yesterday, TestHelper.Tomorrow, initialSeats);
        var booking = new Booking(Guid.NewGuid(), BookingStatus.Pending, TestHelper.Yesterday);

        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Events.ExistsAsync(booking.EventId, cancellationToken: TestContext.Current.CancellationToken))
            .Throws(expectedMainException);

        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Events.GetByIdAsync(booking.EventId, cancellationToken: TestContext.Current.CancellationToken))
            .Throws(innerException);

        // Act

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.ProcessBookingAsync(booking, cancellationToken: TestContext.Current.CancellationToken));

        // Arrange
        Assert.Equal(expectedBookingStatus, booking.Status);
        Assert.Equal(initialSeats, @event.AvailableSeats);
        Assert.IsType<InvalidOperationException>(exception);
        Assert.NotNull(booking.ProcessedAt);
        Assert.NotEqual(default(DateTimeOffset), booking.ProcessedAt);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.ExistsAsync(booking.EventId, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.GetByIdAsync(booking.EventId, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.Update(It.IsAny<Event>()), Times.Never);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Bookings.Update(It.IsAny<Booking>()), Times.Once);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }
}
