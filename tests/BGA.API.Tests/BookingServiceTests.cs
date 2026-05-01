using BGA.API.Application.Exceptions;
using BGA.API.Application.Services.Implementations;
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
    private readonly Mock<IBookingRepository> _bookingRepository;
    private readonly Mock<IEventRepository> _eventRepository;
    private readonly Mock<ILogger<BookingService>> _logger;
    private readonly FakeTimeProvider _timeProvider;
    private readonly BookingService _service;

    public BookingServiceTests()
    {
        _bookingRepository = new Mock<IBookingRepository>();
        _eventRepository = new Mock<IEventRepository>();
        _logger = new Mock<ILogger<BookingService>>();
        _timeProvider = new FakeTimeProvider();
        _service = new BookingService(_bookingRepository.Object, _eventRepository.Object, _logger.Object, _timeProvider);
    }

    [Fact]
    public async Task CreateBookingAsync_WithExistsEvent_ReturnsBooking()
    {
        // Arrange
        var expectedBookingStatus = BookingStatus.Pending;
        var @event = new Event("title", "description", TestHelper.Yesterday, TestHelper.Tomorrow, int.MaxValue);

        _eventRepository
            .Setup(repository => repository.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(@event);

        // Act
        var result = await _service.CreateBookingAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedBookingStatus, result.Status);
        Assert.Equal(_timeProvider.GetUtcNow(), result.CreatedAt);

        _eventRepository
            .Verify(repository => repository.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _eventRepository
            .Verify(repository => repository.UpdateAsync(It.IsAny<Event>(), cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _bookingRepository
            .Verify(repository => repository.CreateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsync_WithExistsEvent_DecrementAvailableSeats_And_ReturnsBooking()
    {
        // Arrange
        var initialTotalSeats = 5;
        var expectedAvailableSeats = initialTotalSeats - 1;
        var @event = new Event("title", "description", TestHelper.Yesterday, TestHelper.Tomorrow, initialTotalSeats);

        _eventRepository
            .Setup(repository => repository.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(@event);

        // Act
        var result = await _service.CreateBookingAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedAvailableSeats, @event.AvailableSeats);

        _eventRepository
            .Verify(repository => repository.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _eventRepository
            .Verify(repository => repository.UpdateAsync(It.IsAny<Event>(), cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _bookingRepository
            .Verify(repository => repository.CreateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsync_WithExistsEvent_CreateBeforeLimit_AllBookingsWithUniqueId_ReturnsBooking()
    {
        // Arrange
        var initialTotalSeats = 5;
        var @event = new Event("title", "description", TestHelper.Yesterday, TestHelper.Tomorrow, initialTotalSeats);
        List<Guid> createdBookingIds = [];

        _eventRepository
            .Setup(repository => repository.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(@event);

        // Act & Assert
        for (var i = 0; i < initialTotalSeats; i++)
        {
            _bookingRepository
                .Setup(repository => repository.CreateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken))
                .Callback<Booking, CancellationToken>((booking, cancellationToken) =>
                {
                    booking.Id = Guid.NewGuid();
                });

            var result = await _service.CreateBookingAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken);
            Assert.NotNull(result);
            Assert.DoesNotContain(result.Id, createdBookingIds);

            createdBookingIds.Add(result.Id);
        }

        _eventRepository
            .Verify(repository => repository.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken), Times.Exactly(initialTotalSeats));

        _eventRepository
            .Verify(repository => repository.UpdateAsync(It.IsAny<Event>(), cancellationToken: TestContext.Current.CancellationToken), Times.Exactly(initialTotalSeats));

        _bookingRepository
            .Verify(repository => repository.CreateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken), Times.Exactly(initialTotalSeats));
    }

    [Fact]
    public async Task CreateBookingAsync_TwiceWithTheSameEventIdAndExistsEvent_ReturnsDifferentBookingIds()
    {
        // Arrange
        var @event = new Event("title", "description", TestHelper.Yesterday, TestHelper.Tomorrow, int.MaxValue);
        _eventRepository
            .Setup(repository => repository.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(@event);

        var capturedEntities = new List<Booking>();

        _bookingRepository
            .Setup(repository => repository.CreateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken))
            .Callback<Booking, CancellationToken>((booking, cancellationToken) =>
            {
                booking.Id = Guid.NewGuid();
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

        _eventRepository
            .Verify(repository => repository.UpdateAsync(It.IsAny<Event>(), cancellationToken: TestContext.Current.CancellationToken), Times.Exactly(2));

        _eventRepository
            .Verify(repository => repository.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken), Times.Exactly(2));

        _bookingRepository
            .Verify(repository => repository.CreateAsync(capturedEntities[0], cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _bookingRepository
            .Verify(repository => repository.CreateAsync(capturedEntities[1], cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsync_WhenEventRemoved_ThrowNotFoundException()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _eventRepository
            .Setup(repository => repository.GetByIdAsync(eventId, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync((Event)null!);

        // Act
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            async () => await _service.CreateBookingAsync(eventId, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal("Event not found", exception.Message);

        _eventRepository
            .Verify(repository => repository.GetByIdAsync(eventId, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _eventRepository
            .Verify(repository => repository.UpdateAsync(It.IsAny<Event>(), cancellationToken: TestContext.Current.CancellationToken), Times.Never);

        _bookingRepository
            .Verify(repository => repository.CreateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken), Times.Never);
    }

    [Fact]
    public async Task CreateBookingAsync_WithRepositoryThrowsException()
    {
        // Arrange
        var @event = new Event("title", "description", TestHelper.Yesterday, TestHelper.Tomorrow, int.MaxValue);

        _eventRepository
            .Setup(repository => repository.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(@event);

        _bookingRepository
            .Setup(repository => repository.CreateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken))
            .ThrowsAsync(new Exception());

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            async () => await _service.CreateBookingAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken));

        _eventRepository
            .Verify(repository => repository.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _eventRepository
            .Verify(repository => repository.UpdateAsync(It.IsAny<Event>(), cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _bookingRepository
            .Verify(repository => repository.CreateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsync_AfterLastAvailableSeat_NoAvailableSeats_ThrowNoAvailableSeatsException()
    {
        // Arrange
        var availableSeats = 1;
        var @event = new Event("title", "description", TestHelper.Yesterday, TestHelper.Tomorrow, availableSeats);

        _eventRepository
            .Setup(repository => repository.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(@event);

        await _service.CreateBookingAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<NoAvailableSeatsException>(
            async () => await _service.CreateBookingAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal("No available seats for this event", exception.Message);

        _eventRepository
            .Verify(repository => repository.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken), Times.Exactly(2));

        _eventRepository
            .Verify(repository => repository.UpdateAsync(It.IsAny<Event>(), cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _bookingRepository
            .Verify(repository => repository.CreateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetBookingByIdAsync_WithExistingBooking_ReturnsBooking()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var booking = new Booking
        {
            Id = bookingId,
            EventId = Guid.NewGuid(),
            Status = BookingStatus.Pending,
            CreatedAt = _timeProvider.GetUtcNow()
        };

        _bookingRepository
            .Setup(repository => repository.GetByIdAsync(bookingId, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(booking);

        // Act
        var result = await _service.GetBookingByIdAsync(bookingId, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(booking.Id, result.Id);
        Assert.Equal(booking.EventId, result.EventId);
        Assert.Equal(booking.Status, result.Status);
        Assert.Equal(booking.CreatedAt, result.CreatedAt);
        Assert.Equal(booking.ProcessedAt, result.ProcessedAt);

        _bookingRepository
            .Verify(repository => repository.GetByIdAsync(bookingId, cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetBookingByIdAsync_WithExistingBooking_ReturnsBookingWithConfirmedStatus()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var booking = new Booking
        {
            Id = bookingId,
            EventId = Guid.NewGuid(),
            Status = BookingStatus.Confirmed,
            CreatedAt = _timeProvider.GetUtcNow()
        };

        _bookingRepository
            .Setup(repository => repository.GetByIdAsync(bookingId, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(booking);

        // Act
        var result = await _service.GetBookingByIdAsync(bookingId, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(booking.Status, result.Status);

        _bookingRepository
            .Verify(repository => repository.GetByIdAsync(bookingId, cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetBookingByIdAsync_WithExistingBooking_ReturnsBookingWithRejectedStatus()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var booking = new Booking
        {
            Id = bookingId,
            EventId = Guid.NewGuid(),
            Status = BookingStatus.Rejected,
            CreatedAt = _timeProvider.GetUtcNow()
        };

        _bookingRepository
            .Setup(repository => repository.GetByIdAsync(bookingId, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(booking);

        // Act
        var result = await _service.GetBookingByIdAsync(bookingId, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(booking.Status, result.Status);

        _bookingRepository
            .Verify(repository => repository.GetByIdAsync(bookingId, cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetBookingByIdAsync_WithNotExistingBooking_ThrowNotFoundException()
    {
        // Arrange
        var bookingId = Guid.NewGuid();

        _bookingRepository
            .Setup(repository => repository.GetByIdAsync(bookingId, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync((Booking)null!);

        // Act
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            async () => await _service.GetBookingByIdAsync(bookingId, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal("Booking not found", exception.Message);

        _bookingRepository
            .Verify(repository => repository.GetByIdAsync(bookingId, cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetBookingByIdAsync_WithRepositoryThrowsException()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        _bookingRepository
            .Setup(repository => repository.GetByIdAsync(bookingId, cancellationToken: TestContext.Current.CancellationToken))
            .ThrowsAsync(new Exception());

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            async () => await _service.GetBookingByIdAsync(bookingId, cancellationToken: TestContext.Current.CancellationToken));

        _bookingRepository
            .Verify(repository => repository.GetByIdAsync(bookingId, cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task ProcessBookingAsync_WithExistsEvent_BookingConfirmed()
    {
        // Arrange
        var expectedBookingStatus = BookingStatus.Confirmed;
        var booking = new Booking()
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            Status = BookingStatus.Pending,
            CreatedAt = TestHelper.Yesterday
        };

        _eventRepository
            .Setup(repository => repository.ExistsAsync(booking.EventId, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(true);

        // Act
        await _service.ProcessBookingAsync(booking, cancellationToken: TestContext.Current.CancellationToken);

        // Arrange
        Assert.Equal(expectedBookingStatus, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
        Assert.NotEqual(default(DateTimeOffset), booking.ProcessedAt);

        _eventRepository
            .Verify(repository => repository.ExistsAsync(booking.EventId, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _eventRepository
            .Verify(repository => repository.GetByIdAsync(booking.EventId, cancellationToken: TestContext.Current.CancellationToken), Times.Never);

        _eventRepository
            .Verify(repository => repository.UpdateAsync(It.IsAny<Event>(), cancellationToken: TestContext.Current.CancellationToken), Times.Never);

        _bookingRepository
            .Verify(repository => repository.UpdateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task ProcessBookingAsync_WithNotExistsEvent_BookingRejected()
    {
        // Arrange
        var expectedBookingStatus = BookingStatus.Rejected;
        var booking = new Booking()
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            Status = BookingStatus.Pending,
            CreatedAt = TestHelper.Yesterday
        };

        _eventRepository
            .Setup(repository => repository.ExistsAsync(booking.EventId, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(false);

        // Act
        await _service.ProcessBookingAsync(booking, cancellationToken: TestContext.Current.CancellationToken);

        // Arrange
        Assert.Equal(expectedBookingStatus, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
        Assert.NotEqual(default(DateTimeOffset), booking.ProcessedAt);

        _eventRepository
            .Verify(repository => repository.ExistsAsync(booking.EventId, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _eventRepository
            .Verify(repository => repository.GetByIdAsync(booking.EventId, cancellationToken: TestContext.Current.CancellationToken), Times.Never);

        _eventRepository
            .Verify(repository => repository.UpdateAsync(It.IsAny<Event>(), cancellationToken: TestContext.Current.CancellationToken), Times.Never);

        _bookingRepository
            .Verify(repository => repository.UpdateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task ProcessBookingAsync_WhenEventRepositoryThrowException_BookingRejectedAndEventReleasedSeats_ThrowsException()
    {
        // Arrange
        var initialSeats = 2;
        var expectedException = new InvalidOperationException();
        var expectedBookingStatus = BookingStatus.Rejected;
        var @event = new Event("title", "description", TestHelper.Yesterday, TestHelper.Tomorrow, initialSeats);
        var booking = new Booking()
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            Status = BookingStatus.Pending,
            CreatedAt = TestHelper.Yesterday
        };

        _eventRepository
            .Setup(repository => repository.ExistsAsync(booking.EventId, cancellationToken: TestContext.Current.CancellationToken))
            .Throws(expectedException);

        _eventRepository
            .Setup(repository => repository.GetByIdAsync(booking.EventId, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(@event);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.ProcessBookingAsync(booking, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(expectedBookingStatus, booking.Status);
        Assert.Equal(initialSeats, @event.AvailableSeats);
        Assert.NotNull(booking.ProcessedAt);
        Assert.NotEqual(default(DateTimeOffset), booking.ProcessedAt);

        _eventRepository
            .Verify(repository => repository.ExistsAsync(booking.EventId, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _eventRepository
            .Verify(repository => repository.GetByIdAsync(booking.EventId, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _eventRepository
            .Verify(repository => repository.UpdateAsync(It.IsAny<Event>(), cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _bookingRepository
            .Verify(repository => repository.UpdateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken), Times.Once);
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
        var booking = new Booking()
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            Status = BookingStatus.Pending,
            CreatedAt = TestHelper.Yesterday
        };

        _eventRepository
            .Setup(repository => repository.ExistsAsync(booking.EventId, cancellationToken: TestContext.Current.CancellationToken))
            .Throws(expectedMainException);

        _eventRepository
            .Setup(repository => repository.GetByIdAsync(booking.EventId, cancellationToken: TestContext.Current.CancellationToken))
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

        _eventRepository
            .Verify(repository => repository.ExistsAsync(booking.EventId, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _eventRepository
            .Verify(repository => repository.GetByIdAsync(booking.EventId, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _eventRepository
            .Verify(repository => repository.UpdateAsync(It.IsAny<Event>(), cancellationToken: TestContext.Current.CancellationToken), Times.Never);

        _bookingRepository
            .Verify(repository => repository.UpdateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }
}
