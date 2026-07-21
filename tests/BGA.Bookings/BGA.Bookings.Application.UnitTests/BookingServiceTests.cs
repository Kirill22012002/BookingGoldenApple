using BGA.Bookings.Application.Messaging;
using BGA.Bookings.Application.Repositories;
using BGA.Bookings.Application.Services.Implementations;
using BGA.Bookings.Application.UnitTests.Helpers;
using BGA.Contracts.Bookings;
using BGA.Bookings.Domain.Exceptions;
using BGA.Bookings.Domain.Models;
using BGA.Bookings.Domain.Models.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace BGA.Bookings.Application.UnitTests;

public class BookingServiceTests
{
    private readonly Mock<IBookingRepository> _bookingRepositoryMock = new();
    private readonly Mock<IBookingConfirmedPublisher> _bookingConfirmedPublisherMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<BookingService>> _logger = new();
    private readonly FakeTimeProvider _timeProvider = new();
    private readonly BookingService _service;

    public BookingServiceTests()
    {
        _timeProvider.SetUtcNow(TestHelper.Now);
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.Bookings).Returns(_bookingRepositoryMock.Object);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _service = new BookingService(_bookingConfirmedPublisherMock.Object, _unitOfWork.Object, _logger.Object, _timeProvider);
    }

    [Fact]
    public async Task CreateBookingAsync_WhenUserHasFreeLimit_ReturnsPendingBooking()
    {
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _bookingRepositoryMock.Setup(repository => repository.CountActiveByUserIdAsync(userId, TestContext.Current.CancellationToken)).ReturnsAsync(0);

        var result = await _service.CreateBookingAsync(eventId, userId, TestContext.Current.CancellationToken);

        Assert.Equal(userId, result.UserId);
        Assert.Equal(eventId, result.EventId);
        Assert.Equal(BookingStatus.Pending, result.Status);
        Assert.Equal(TestHelper.Now, result.CreatedAt);
        _bookingRepositoryMock.Verify(repository => repository.CreateAsync(It.IsAny<Booking>(), TestContext.Current.CancellationToken), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsync_WhenUserReachedActiveBookingsLimit_ThrowsBookingLimitExceededException()
    {
        var userId = Guid.NewGuid();
        _bookingRepositoryMock.Setup(repository => repository.CountActiveByUserIdAsync(userId, TestContext.Current.CancellationToken)).ReturnsAsync(10);

        var exception = await Assert.ThrowsAsync<BookingLimitExceededException>(() =>
            _service.CreateBookingAsync(Guid.NewGuid(), userId, TestContext.Current.CancellationToken));

        Assert.Equal("User cannot have more than 10 active bookings.", exception.Message);
    }

    [Fact]
    public async Task CancelBookingAsync_WhenBookingBelongsToUser_CancelsBooking()
    {
        var userId = Guid.NewGuid();
        var booking = new Booking(Guid.NewGuid(), userId, BookingStatus.Pending, TestHelper.Now);
        _bookingRepositoryMock.Setup(repository => repository.GetByIdAsync(booking.Id, TestContext.Current.CancellationToken)).ReturnsAsync(booking);

        await _service.CancelBookingAsync(booking.Id, userId, UserRole.User, TestContext.Current.CancellationToken);

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        _bookingRepositoryMock.Verify(repository => repository.Update(booking), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task CancelBookingAsync_WhenUserCancelsAnotherUsersBooking_ThrowsOperationForbiddenException()
    {
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid(), BookingStatus.Pending, TestHelper.Now);
        _bookingRepositoryMock.Setup(repository => repository.GetByIdAsync(booking.Id, TestContext.Current.CancellationToken)).ReturnsAsync(booking);

        var exception = await Assert.ThrowsAsync<OperationForbiddenException>(() =>
            _service.CancelBookingAsync(booking.Id, Guid.NewGuid(), UserRole.User, TestContext.Current.CancellationToken));

        Assert.Equal("You do not have permission to cancel this booking.", exception.Message);
    }

    [Fact]
    public async Task GetBookingByIdAsync_WhenBookingMissing_ThrowsNotFoundException()
    {
        var bookingId = Guid.NewGuid();
        _bookingRepositoryMock.Setup(repository => repository.GetByIdAsync(bookingId, TestContext.Current.CancellationToken)).ReturnsAsync((Booking)null!);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.GetBookingByIdAsync(bookingId, TestContext.Current.CancellationToken));

        Assert.Equal("Booking not found", exception.Message);
    }

    [Fact]
    public async Task ProcessBookingAsync_WhenBookingIsPending_ConfirmsBooking()
    {
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid(), BookingStatus.Pending, TestHelper.Now);

        await _service.ProcessBookingAsync(booking, TestContext.Current.CancellationToken);

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
        _bookingRepositoryMock.Verify(repository => repository.Update(booking), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken), Times.Once);
        _bookingConfirmedPublisherMock.Verify(
            publisher => publisher.PublishAsync(
                It.Is<BookingConfirmed>(message =>
                    message.BookingId == booking.Id &&
                    message.EventId == booking.EventId &&
                    message.UserId == booking.UserId &&
                    message.SeatsCount == 1 &&
                    message.ConfirmedAt == booking.ProcessedAt),
                TestContext.Current.CancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task ProcessBookingAsync_WhenBookingAlreadyProcessed_DoesNothing()
    {
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid(), BookingStatus.Confirmed, TestHelper.Now);

        await _service.ProcessBookingAsync(booking, TestContext.Current.CancellationToken);

        _bookingRepositoryMock.Verify(repository => repository.Update(It.IsAny<Booking>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken), Times.Never);
        _bookingConfirmedPublisherMock.Verify(
            publisher => publisher.PublishAsync(It.IsAny<BookingConfirmed>(), TestContext.Current.CancellationToken),
            Times.Never);
    }

    [Fact]
    public async Task ProcessBookingAsync_WhenSavingFails_DoesNotPublishEvent()
    {
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid(), BookingStatus.Pending, TestHelper.Now);
        _unitOfWork
            .Setup(unitOfWork => unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken))
            .ThrowsAsync(new InvalidOperationException("Database write failed."));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ProcessBookingAsync(booking, TestContext.Current.CancellationToken));

        _bookingConfirmedPublisherMock.Verify(
            publisher => publisher.PublishAsync(It.IsAny<BookingConfirmed>(), TestContext.Current.CancellationToken),
            Times.Never);
    }

    [Fact]
    public async Task ProcessBookingAsync_WhenChangesWereNotSaved_DoesNotPublishEvent()
    {
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid(), BookingStatus.Pending, TestHelper.Now);
        _unitOfWork
            .Setup(unitOfWork => unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync(false);

        await _service.ProcessBookingAsync(booking, TestContext.Current.CancellationToken);

        _bookingConfirmedPublisherMock.Verify(
            publisher => publisher.PublishAsync(It.IsAny<BookingConfirmed>(), TestContext.Current.CancellationToken),
            Times.Never);
    }
}
