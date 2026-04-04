using BGA.API.Application;
using BGA.API.Application.Services.Implementations;
using BGA.API.Infrastructure.Models;
using BGA.API.Infrastructure.Models.Enums;
using BGA.API.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace BGA.API.Tests;

public class BookingServiceTests
{
    private readonly Mock<IBookingRepository> _bookingRepository;
    private readonly Mock<IEventRepository> _eventRepository;
    private readonly FakeTimeProvider _timeProvider;
    private readonly BookingService _service;

    public BookingServiceTests()
    {
        _bookingRepository = new Mock<IBookingRepository>();
        _eventRepository = new Mock<IEventRepository>();
        _timeProvider = new FakeTimeProvider();
        _service = new BookingService(_bookingRepository.Object, _eventRepository.Object, _timeProvider);
    }

    [Fact]
    public async Task CreateBookingAsync_WithExistsEvent_ReturnsServiceResponseWithSuccessAndCorrectBooking()
    {
        // Arrange
        var expectedBookingStatus = BookingStatus.Pending;
        var eventId = Guid.NewGuid();
        _eventRepository
            .Setup(repository => repository.ExistsAsync(eventId, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(true);

        _bookingRepository
            .Setup(repository => repository.CreateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CreateBookingAsync(eventId, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<ServiceResponse<Booking>>(result);
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);
        Assert.Equal(expectedBookingStatus, result.Data.Status);
        Assert.Equal(_timeProvider.GetUtcNow(), result.Data.CreatedAt);

        _eventRepository
            .Verify(repository => repository.ExistsAsync(eventId, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _bookingRepository
            .Verify(repository => repository.CreateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsync_WithNotExistsEvent_ReturnsServiceResponseWithNotSuccessAndErrorMessage()
    {
        // Arrange
        var expectedExceptionMessage = "Event not found";
        var eventId = Guid.NewGuid();
        _eventRepository
            .Setup(repository => repository.ExistsAsync(eventId, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(false);

        // Act
        var result = await _service.CreateBookingAsync(eventId, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<ServiceResponse<Booking>>(result);
        Assert.False(result.Succeeded);
        Assert.Null(result.Data);
        Assert.Contains(expectedExceptionMessage, result.Errors);

        _eventRepository
            .Verify(repository => repository.ExistsAsync(eventId, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _bookingRepository
            .Verify(repository => repository.CreateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken), Times.Never);
    }

    [Fact]
    public async Task CreateBookingAsync_WithRepositoryReturnsFalse_ReturnsServiceResponseWithNotSuccessAndErrorMessage()
    {
        // Arrange
        var expectedExceptionMessage = "Cannot create booking";
        var eventId = Guid.NewGuid();
        _eventRepository
            .Setup(repository => repository.ExistsAsync(eventId, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(true);

        _bookingRepository
            .Setup(repository => repository.CreateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(false);

        // Act
        var result = await _service.CreateBookingAsync(eventId, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<ServiceResponse<Booking>>(result);
        Assert.False(result.Succeeded);
        Assert.Contains(expectedExceptionMessage, result.Errors);

        _eventRepository
            .Verify(repository => repository.ExistsAsync(eventId, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _bookingRepository
            .Verify(repository => repository.CreateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsync_WithRepositoryThrowsException_ReturnsServiceResponseWithNotSuccessAndErrorMessage()
    {
        // Arrange
        var expectedExceptionMessage = "Database error";
        var eventId = Guid.NewGuid();
        _eventRepository
            .Setup(repository => repository.ExistsAsync(eventId, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(true);

        _bookingRepository
            .Setup(repository => repository.CreateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken))
            .ThrowsAsync(new Exception(expectedExceptionMessage));

        // Act
        var result = await _service.CreateBookingAsync(eventId, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<ServiceResponse<Booking>>(result);
        Assert.False(result.Succeeded);
        Assert.Contains(expectedExceptionMessage, result.Errors);

        _eventRepository
            .Verify(repository => repository.ExistsAsync(eventId, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

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
            CreatedAt = _timeProvider.GetUtcNow(),
            ProcessedAt = null
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
            CreatedAt = _timeProvider.GetUtcNow(),
            ProcessedAt = null
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
            CreatedAt = _timeProvider.GetUtcNow(),
            ProcessedAt = null
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
}
