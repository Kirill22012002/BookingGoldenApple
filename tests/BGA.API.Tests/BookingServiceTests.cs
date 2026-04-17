using BGA.API.Application;
using BGA.API.Application.Services.Implementations;
using BGA.API.Infrastructure.Models;
using BGA.API.Infrastructure.Models.Enums;
using BGA.API.Infrastructure.Repositories.Interfaces;
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
    public async Task CreateBookingAsync_WithExistsEvent_ReturnsServiceResponseWithSuccessAndCorrectBooking()
    {
        // Arrange
        var expectedBookingStatus = BookingStatus.Pending;
        var @event = new Event("title", "description", DateTimeOffset.MinValue, DateTimeOffset.MaxValue, int.MaxValue);

        _eventRepository
            .Setup(repository => repository.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(@event);

        _bookingRepository
            .Setup(repository => repository.CreateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CreateBookingAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<ServiceResponse<Booking>>(result);
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);
        Assert.Equal(expectedBookingStatus, result.Data.Status);
        Assert.Equal(_timeProvider.GetUtcNow(), result.Data.CreatedAt);

        _eventRepository
            .Verify(repository => repository.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _eventRepository
            .Verify(repository => repository.UpdateAsync(It.IsAny<Event>(), cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _bookingRepository
            .Verify(repository => repository.CreateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsync_WithExistsEvent_DecrementAvailableSeats_And_ReturnsServiceResponseWithSuccessAndCorrectBooking()
    {
        // Arrange
        var initialTotalSeats = 5;
        var expectedAvailableSeats = initialTotalSeats - 1;
        var @event = new Event("title", "description", DateTimeOffset.MinValue, DateTimeOffset.MaxValue, initialTotalSeats);

        _eventRepository
            .Setup(repository => repository.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(@event);

        _bookingRepository
            .Setup(repository => repository.CreateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CreateBookingAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<ServiceResponse<Booking>>(result);
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);
        Assert.Equal(expectedAvailableSeats, @event.AvailableSeats);

        _eventRepository
            .Verify(repository => repository.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _eventRepository
            .Verify(repository => repository.UpdateAsync(It.IsAny<Event>(), cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _bookingRepository
            .Verify(repository => repository.CreateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsync_WithExistsEvent_CreateBeforeLimit_AllBookingsWithUniqueId_ReturnsServiceResponseWithSuccessAndCorrectBooking()
    {
        // Arrange
        var initialTotalSeats = 5;
        var @event = new Event("title", "description", DateTimeOffset.MinValue, DateTimeOffset.MaxValue, initialTotalSeats);
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
                })
                .ReturnsAsync(true);

            var result = await _service.CreateBookingAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken);
            Assert.IsType<ServiceResponse<Booking>>(result);
            Assert.True(result.Succeeded);
            Assert.NotNull(result.Data);
            Assert.DoesNotContain(result.Data.Id, createdBookingIds);

            createdBookingIds.Add(result.Data.Id);
        }

        _eventRepository
            .Verify(repository => repository.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken), Times.Exactly(initialTotalSeats));

        _eventRepository
            .Verify(repository => repository.UpdateAsync(It.IsAny<Event>(), cancellationToken: TestContext.Current.CancellationToken), Times.Exactly(initialTotalSeats));

        _bookingRepository
            .Verify(repository => repository.CreateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken), Times.Exactly(initialTotalSeats));
    }

    [Fact]
    public async Task CreateBookingAsync_TwiceWithTheSameEventIdAndExistsEvent_ReturnsServiceResponseWithSuccessAndDifferentBookingIds()
    {
        // Arrange
        var @event = new Event("title", "description", DateTimeOffset.MinValue, DateTimeOffset.MaxValue, int.MaxValue);
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
            })
            .ReturnsAsync(true);

        // Act
        var firstBookingResult = await _service.CreateBookingAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken);
        var secondBookingResult = await _service.CreateBookingAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<ServiceResponse<Booking>>(firstBookingResult);
        Assert.IsType<ServiceResponse<Booking>>(secondBookingResult);
        Assert.True(firstBookingResult.Succeeded);
        Assert.True(secondBookingResult.Succeeded);
        Assert.NotNull(firstBookingResult.Data);
        Assert.NotNull(secondBookingResult.Data);
        Assert.Equal(firstBookingResult.Data.EventId, secondBookingResult.Data.EventId);
        Assert.NotEqual(firstBookingResult.Data.Id, secondBookingResult.Data.Id);
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
    public async Task CreateBookingAsync_WhenEventRemoved_ReturnsServiceResponseWithNotSuccessAndErrorMessage()
    {
        // Arrange
        var expectedExceptionMessage = "Event not found";
        var eventId = Guid.NewGuid();
        _eventRepository
            .Setup(repository => repository.GetByIdAsync(eventId, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync((Event)null!);

        // Act
        var result = await _service.CreateBookingAsync(eventId, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<ServiceResponse<Booking>>(result);
        Assert.False(result.Succeeded);
        Assert.Null(result.Data);
        Assert.Contains(expectedExceptionMessage, result.Errors);

        _eventRepository
            .Verify(repository => repository.GetByIdAsync(eventId, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _eventRepository
            .Verify(repository => repository.UpdateAsync(It.IsAny<Event>(), cancellationToken: TestContext.Current.CancellationToken), Times.Never);

        _bookingRepository
            .Verify(repository => repository.CreateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken), Times.Never);
    }

    [Fact]
    public async Task CreateBookingAsync_WithRepositoryReturnsFalse_ReturnsServiceResponseWithNotSuccessAndErrorMessage()
    {
        // Arrange
        var expectedExceptionMessage = "Cannot create booking";
        var @event = new Event("title", "description", DateTimeOffset.MinValue, DateTimeOffset.MaxValue, int.MaxValue);
        _eventRepository
            .Setup(repository => repository.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(@event);

        _bookingRepository
            .Setup(repository => repository.CreateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(false);

        // Act
        var result = await _service.CreateBookingAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<ServiceResponse<Booking>>(result);
        Assert.False(result.Succeeded);
        Assert.Contains(expectedExceptionMessage, result.Errors);

        _eventRepository
            .Verify(repository => repository.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _eventRepository
            .Verify(repository => repository.UpdateAsync(It.IsAny<Event>(), cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _bookingRepository
            .Verify(repository => repository.CreateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsync_WithRepositoryThrowsException_ReturnsServiceResponseWithNotSuccessAndErrorMessage()
    {
        // Arrange
        var expectedExceptionMessage = "Database error";
        var @event = new Event("title", "description", DateTimeOffset.MinValue, DateTimeOffset.MaxValue, int.MaxValue);

        _eventRepository
            .Setup(repository => repository.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(@event);

        _bookingRepository
            .Setup(repository => repository.CreateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken))
            .ThrowsAsync(new Exception(expectedExceptionMessage));

        // Act
        var result = await _service.CreateBookingAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<ServiceResponse<Booking>>(result);
        Assert.False(result.Succeeded);
        Assert.Contains(expectedExceptionMessage, result.Errors);

        _eventRepository
            .Verify(repository => repository.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _eventRepository
            .Verify(repository => repository.UpdateAsync(It.IsAny<Event>(), cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _bookingRepository
            .Verify(repository => repository.CreateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsync_WithNoAvailableSeats_ReturnsServiceResponseWithNotSuccessAndErrorMessage()
    {
        // Arrange
        var expectedExceptionMessage = "No available seats for this event";
        var expectedServiceErrorType = ServiceErrorType.Conflict;
        var availableSeats = 0;
        var @event = new Event("title", "description", DateTimeOffset.MinValue, DateTimeOffset.MaxValue, availableSeats);

        _eventRepository
            .Setup(repository => repository.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(@event);

        // Act
        var result = await _service.CreateBookingAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<ServiceResponse<Booking>>(result);
        Assert.False(result.Succeeded);
        Assert.Contains(expectedExceptionMessage, result.Errors);
        Assert.Equal(expectedServiceErrorType, result.ErrorType);

        _eventRepository
            .Verify(repository => repository.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _eventRepository
            .Verify(repository => repository.UpdateAsync(It.IsAny<Event>(), cancellationToken: TestContext.Current.CancellationToken), Times.Never);

        _bookingRepository
            .Verify(repository => repository.CreateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken), Times.Never);
    }

    [Fact]
    public async Task CreateBookingAsync_AfterLastAvailableSeat_NoAvailableSeats_ReturnsServiceResponseWithNotSuccessAndErrorMessage()
    {
        // Arrange
        var expectedExceptionMessage = "No available seats for this event";
        var expectedServiceErrorType = ServiceErrorType.Conflict;
        var availableSeats = 1;
        var @event = new Event("title", "description", DateTimeOffset.MinValue, DateTimeOffset.MaxValue, availableSeats);

        _eventRepository
            .Setup(repository => repository.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(@event);

        _bookingRepository
            .Setup(repository => repository.CreateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(true);

        // Act & Assert
        var firstResponse = await _service.CreateBookingAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken);
        Assert.IsType<ServiceResponse<Booking>>(firstResponse);
        Assert.True(firstResponse.Succeeded);
        Assert.NotNull(firstResponse.Data);

        var secondResponse = await _service.CreateBookingAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken);
        Assert.IsType<ServiceResponse<Booking>>(secondResponse);
        Assert.False(secondResponse.Succeeded);
        Assert.Null(secondResponse.Data);
        Assert.Contains(expectedExceptionMessage, secondResponse.Errors);
        Assert.Equal(expectedServiceErrorType, secondResponse.ErrorType);

        _eventRepository
            .Verify(repository => repository.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken), Times.Exactly(2));

        _eventRepository
            .Verify(repository => repository.UpdateAsync(It.IsAny<Event>(), cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _bookingRepository
            .Verify(repository => repository.CreateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetBookingByIdAsync_WithExistingBooking_ReturnsServiceResponseWithSuccessAndCorrectBooking()
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
        Assert.IsType<ServiceResponse<Booking>>(result);
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);
        Assert.Equal(booking.Id, result.Data.Id);
        Assert.Equal(booking.EventId, result.Data.EventId);
        Assert.Equal(booking.Status, result.Data.Status);
        Assert.Equal(booking.CreatedAt, result.Data.CreatedAt);
        Assert.Equal(booking.ProcessedAt, result.Data.ProcessedAt);

        _bookingRepository
            .Verify(repository => repository.GetByIdAsync(bookingId, cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetBookingByIdAsync_WithExistingBooking_ReturnsServiceResponseWithSuccessAndCorrectBookingWithConfirmedStatus()
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
        Assert.IsType<ServiceResponse<Booking>>(result);
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);
        Assert.Equal(booking.Status, result.Data.Status);

        _bookingRepository
            .Verify(repository => repository.GetByIdAsync(bookingId, cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetBookingByIdAsync_WithExistingBooking_ReturnsServiceResponseWithSuccessAndCorrectBookingWithRejectedStatus()
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
        Assert.IsType<ServiceResponse<Booking>>(result);
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);
        Assert.Equal(booking.Status, result.Data.Status);

        _bookingRepository
            .Verify(repository => repository.GetByIdAsync(bookingId, cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetBookingByIdAsync_WithNotExistingBooking_ReturnsServiceResponseWithNotSuccess()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var expectedExceptionMessage = "Booking not found";

        _bookingRepository
            .Setup(repository => repository.GetByIdAsync(bookingId, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync((Booking)null!);

        // Act
        var result = await _service.GetBookingByIdAsync(bookingId, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<ServiceResponse<Booking>>(result);
        Assert.False(result.Succeeded);
        Assert.Contains(expectedExceptionMessage, result.Errors);

        _bookingRepository
            .Verify(repository => repository.GetByIdAsync(bookingId, cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetBookingByIdAsync_WithRepositoryThrowsException_ReturnsServiceResponseWithNotSuccessAndErrorMessage()
    {
        // Arrange
        var expectedExceptionMessage = "Database error";
        var bookingId = Guid.NewGuid();
        _bookingRepository
            .Setup(repository => repository.GetByIdAsync(bookingId, cancellationToken: TestContext.Current.CancellationToken))
            .ThrowsAsync(new Exception(expectedExceptionMessage));

        // Act
        var result = await _service.GetBookingByIdAsync(bookingId, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<ServiceResponse<Booking>>(result);
        Assert.False(result.Succeeded);
        Assert.Contains(expectedExceptionMessage, result.Errors);

        _bookingRepository
            .Verify(repository => repository.GetByIdAsync(bookingId, cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task ProcessBookingAsync_WithExistsEvent_BookingConfirmed_ReturnsServiceResponseWithSuccess()
    {
        // Arrange
        var expectedBookingStatus = BookingStatus.Confirmed;
        var booking = new Booking()
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            Status = BookingStatus.Pending,
            CreatedAt = DateTimeOffset.MinValue
        };

        _eventRepository
            .Setup(repository => repository.ExistsAsync(booking.EventId, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ProcessBookingAsync(booking, cancellationToken: TestContext.Current.CancellationToken);

        // Arrange
        Assert.IsType<ServiceResponse>(result);
        Assert.True(result.Succeeded);
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
    public async Task ProcessBookingAsync_WithNotExistsEvent_BookingRejected_ReturnsServiceResponseWithSuccess()
    {
        // Arrange
        var expectedBookingStatus = BookingStatus.Rejected;
        var booking = new Booking()
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            Status = BookingStatus.Pending,
            CreatedAt = DateTimeOffset.MinValue
        };

        _eventRepository
            .Setup(repository => repository.ExistsAsync(booking.EventId, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(false);

        // Act
        var result = await _service.ProcessBookingAsync(booking, cancellationToken: TestContext.Current.CancellationToken);

        // Arrange
        Assert.IsType<ServiceResponse>(result);
        Assert.True(result.Succeeded);
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
    public async Task ProcessBookingAsync_WhenEventRepositoryThrowException_BookingRejectedAndEventReleasedSeats_ReturnsServiceResponseWithNotSuccess()
    {
        // Arrange
        var initialSeats = 2;
        var expectedException = new InvalidOperationException("Something went wrong");
        var expectedBookingStatus = BookingStatus.Rejected;
        var @event = new Event("title", "description", DateTimeOffset.MinValue, DateTimeOffset.MaxValue, initialSeats);
        var booking = new Booking()
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            Status = BookingStatus.Pending,
            CreatedAt = DateTimeOffset.MinValue
        };

        _eventRepository
            .Setup(repository => repository.ExistsAsync(booking.EventId, cancellationToken: TestContext.Current.CancellationToken))
            .Throws(expectedException);

        _eventRepository
            .Setup(repository => repository.GetByIdAsync(booking.EventId, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(@event);

        // Act
        var result = await _service.ProcessBookingAsync(booking, cancellationToken: TestContext.Current.CancellationToken);

        // Arrange
        Assert.IsType<ServiceResponse>(result);
        Assert.False(result.Succeeded);
        Assert.Equal(expectedBookingStatus, booking.Status);
        Assert.Equal(initialSeats, @event.AvailableSeats);
        Assert.NotNull(booking.ProcessedAt);
        Assert.NotEqual(default(DateTimeOffset), booking.ProcessedAt);
        Assert.Equal(expectedException, result.Exception);
        Assert.Contains(expectedException.Message, result.Errors);

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
    public async Task ProcessBookingAsync_WhenEventRepositoryThrowException_BookingRejectedButEventNotExistsSoWithoutReleasedSeats_ReturnsServiceResponseWithNotSuccess()
    {
        // Arrange
        var initialSeats = 2;
        var expectedMainException = new InvalidOperationException("Something went wrong");
        var innerException = new Exception("Something went wrong during get by id");
        var expectedBookingStatus = BookingStatus.Rejected;
        var @event = new Event("title", "description", DateTimeOffset.MinValue, DateTimeOffset.MaxValue, initialSeats);
        var booking = new Booking()
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            Status = BookingStatus.Pending,
            CreatedAt = DateTimeOffset.MinValue
        };

        _eventRepository
            .Setup(repository => repository.ExistsAsync(booking.EventId, cancellationToken: TestContext.Current.CancellationToken))
            .Throws(expectedMainException);

        _eventRepository
            .Setup(repository => repository.GetByIdAsync(booking.EventId, cancellationToken: TestContext.Current.CancellationToken))
            .Throws(innerException);

        // Act
        var result = await _service.ProcessBookingAsync(booking, cancellationToken: TestContext.Current.CancellationToken);

        // Arrange
        Assert.IsType<ServiceResponse>(result);
        Assert.False(result.Succeeded);
        Assert.Equal(expectedBookingStatus, booking.Status);
        Assert.Equal(initialSeats, @event.AvailableSeats);
        Assert.NotNull(booking.ProcessedAt);
        Assert.NotEqual(default(DateTimeOffset), booking.ProcessedAt);
        Assert.Equal(expectedMainException, result.Exception);
        Assert.Contains(expectedMainException.Message, result.Errors);
        Assert.Contains(innerException.Message, result.Errors);

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
