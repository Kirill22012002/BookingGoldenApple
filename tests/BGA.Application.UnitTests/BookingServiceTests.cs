using BGA.Application.Repositories;
using BGA.Application.Services.Implementations;
using BGA.Application.UnitTests.Helpers;
using BGA.Domain.Exceptions;
using BGA.Domain.Models;
using BGA.Domain.Models.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace BGA.Application.UnitTests;

public class BookingServiceTests
{
    private readonly Mock<IEventRepository> _eventRepositoryMock = new();
    private readonly Mock<IBookingRepository> _bookingRepositoryMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<BookingService>> _logger = new();
    private readonly FakeTimeProvider _timeProvider = new();
    private readonly BookingService _service;

    public BookingServiceTests()
    {
        _timeProvider.SetUtcNow(TestHelper.Now);
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.Events).Returns(_eventRepositoryMock.Object);
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.Bookings).Returns(_bookingRepositoryMock.Object);
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.Users).Returns(_userRepositoryMock.Object);
        _service = new BookingService(_unitOfWork.Object, _logger.Object, _timeProvider);
    }

    [Fact]
    public async Task CreateBookingAsync_WithValidUserAndFutureEvent_ReturnsPendingBooking()
    {
        var user = CreateUser();
        var @event = new Event("title", "description", TestHelper.Tomorrow, TestHelper.Tomorrow.AddHours(2), 3);
        _userRepositoryMock.Setup(repository => repository.GetByIdAsync(user.Id, TestContext.Current.CancellationToken)).ReturnsAsync(user);
        _eventRepositoryMock.Setup(repository => repository.GetByIdAsync(@event.Id, TestContext.Current.CancellationToken)).ReturnsAsync(@event);
        _bookingRepositoryMock.Setup(repository => repository.CountActiveByUserIdAsync(user.Id, TestContext.Current.CancellationToken)).ReturnsAsync(0);

        var result = await _service.CreateBookingAsync(@event.Id, user.Id, TestContext.Current.CancellationToken);

        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(@event.Id, result.EventId);
        Assert.Equal(BookingStatus.Pending, result.Status);
        Assert.Equal(TestHelper.Now, result.CreatedAt);
        Assert.Equal(2, @event.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_WhenEventAlreadyStarted_ThrowsEventAlreadyStartedException()
    {
        var user = CreateUser();
        var @event = new Event("title", "description", TestHelper.Yesterday, TestHelper.Tomorrow, 2);
        _userRepositoryMock.Setup(repository => repository.GetByIdAsync(user.Id, TestContext.Current.CancellationToken)).ReturnsAsync(user);
        _eventRepositoryMock.Setup(repository => repository.GetByIdAsync(@event.Id, TestContext.Current.CancellationToken)).ReturnsAsync(@event);

        var exception = await Assert.ThrowsAsync<EventAlreadyStartedException>(() =>
            _service.CreateBookingAsync(@event.Id, user.Id, TestContext.Current.CancellationToken));

        Assert.Equal("Cannot book an event that has already started.", exception.Message);
    }

    [Fact]
    public async Task CreateBookingAsync_WhenUserReachedActiveBookingsLimit_ThrowsBookingLimitExceededException()
    {
        var user = CreateUser();
        var @event = new Event("title", "description", TestHelper.Tomorrow, TestHelper.Tomorrow.AddHours(2), 3);
        _userRepositoryMock.Setup(repository => repository.GetByIdAsync(user.Id, TestContext.Current.CancellationToken)).ReturnsAsync(user);
        _eventRepositoryMock.Setup(repository => repository.GetByIdAsync(@event.Id, TestContext.Current.CancellationToken)).ReturnsAsync(@event);
        _bookingRepositoryMock.Setup(repository => repository.CountActiveByUserIdAsync(user.Id, TestContext.Current.CancellationToken)).ReturnsAsync(10);

        var exception = await Assert.ThrowsAsync<BookingLimitExceededException>(() =>
            _service.CreateBookingAsync(@event.Id, user.Id, TestContext.Current.CancellationToken));

        Assert.Equal("User cannot have more than 10 active bookings.", exception.Message);
    }

    [Fact]
    public async Task CreateBookingAsync_WhenAnotherUserReachedLimit_DoesNotBlockCurrentUser()
    {
        var limitedUser = CreateUser();
        var currentUser = CreateUser();
        var @event = new Event("title", "description", TestHelper.Tomorrow, TestHelper.Tomorrow.AddHours(2), 3);
        _userRepositoryMock.Setup(repository => repository.GetByIdAsync(currentUser.Id, TestContext.Current.CancellationToken)).ReturnsAsync(currentUser);
        _eventRepositoryMock.Setup(repository => repository.GetByIdAsync(@event.Id, TestContext.Current.CancellationToken)).ReturnsAsync(@event);
        _bookingRepositoryMock.Setup(repository => repository.CountActiveByUserIdAsync(It.IsAny<Guid>(), TestContext.Current.CancellationToken))
            .ReturnsAsync((Guid userId, CancellationToken _) => userId == limitedUser.Id ? 10 : 0);

        var result = await _service.CreateBookingAsync(@event.Id, currentUser.Id, TestContext.Current.CancellationToken);

        Assert.Equal(currentUser.Id, result.UserId);
        Assert.Equal(BookingStatus.Pending, result.Status);
    }

    [Fact]
    public async Task CreateBookingAsync_WhenNoAvailableSeats_ThrowsNoAvailableSeatsException()
    {
        var user = CreateUser();
        var @event = new Event("title", "description", TestHelper.Tomorrow, TestHelper.Tomorrow.AddHours(2), 1);
        @event.TryReserveSeats();
        _userRepositoryMock.Setup(repository => repository.GetByIdAsync(user.Id, TestContext.Current.CancellationToken)).ReturnsAsync(user);
        _eventRepositoryMock.Setup(repository => repository.GetByIdAsync(@event.Id, TestContext.Current.CancellationToken)).ReturnsAsync(@event);
        _bookingRepositoryMock.Setup(repository => repository.CountActiveByUserIdAsync(user.Id, TestContext.Current.CancellationToken)).ReturnsAsync(0);

        var exception = await Assert.ThrowsAsync<NoAvailableSeatsException>(() =>
            _service.CreateBookingAsync(@event.Id, user.Id, TestContext.Current.CancellationToken));

        Assert.Equal("No available seats for this event", exception.Message);
    }

    [Fact]
    public async Task CancelBookingAsync_WhenBookingBelongsToUser_CancelsBookingAndReleasesSeat()
    {
        var user = CreateUser();
        var @event = new Event("title", "description", TestHelper.Tomorrow, TestHelper.Tomorrow.AddHours(2), 2);
        @event.TryReserveSeats();
        var booking = new Booking(@event.Id, user.Id, BookingStatus.Pending, TestHelper.Now);
        _bookingRepositoryMock.Setup(repository => repository.GetByIdAsync(booking.Id, TestContext.Current.CancellationToken)).ReturnsAsync(booking);
        _eventRepositoryMock.Setup(repository => repository.GetByIdAsync(@event.Id, TestContext.Current.CancellationToken)).ReturnsAsync(@event);

        await _service.CancelBookingAsync(booking.Id, user.Id, UserRole.User, TestContext.Current.CancellationToken);

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.Equal(2, @event.AvailableSeats);
        _bookingRepositoryMock.Verify(repository => repository.Update(booking), Times.Once);
        _eventRepositoryMock.Verify(repository => repository.Update(@event), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task CancelBookingAsync_WhenUserCancelsAnotherUsersBooking_ThrowsOperationForbiddenException()
    {
        var owner = CreateUser();
        var anotherUser = CreateUser();
        var booking = new Booking(Guid.NewGuid(), owner.Id, BookingStatus.Pending, TestHelper.Now);
        _bookingRepositoryMock.Setup(repository => repository.GetByIdAsync(booking.Id, TestContext.Current.CancellationToken)).ReturnsAsync(booking);

        var exception = await Assert.ThrowsAsync<OperationForbiddenException>(() =>
            _service.CancelBookingAsync(booking.Id, anotherUser.Id, UserRole.User, TestContext.Current.CancellationToken));

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
    public async Task ProcessBookingAsync_WhenEventExists_ConfirmsBooking()
    {
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid(), BookingStatus.Pending, TestHelper.Now);
        _eventRepositoryMock.Setup(repository => repository.ExistsAsync(booking.EventId, TestContext.Current.CancellationToken)).ReturnsAsync(true);

        await _service.ProcessBookingAsync(booking, TestContext.Current.CancellationToken);

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }

    [Fact]
    public async Task ProcessBookingAsync_WhenEventMissing_RejectsBooking()
    {
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid(), BookingStatus.Pending, TestHelper.Now);
        _eventRepositoryMock.Setup(repository => repository.ExistsAsync(booking.EventId, TestContext.Current.CancellationToken)).ReturnsAsync(false);

        await _service.ProcessBookingAsync(booking, TestContext.Current.CancellationToken);

        Assert.Equal(BookingStatus.Rejected, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }

    private static User CreateUser(UserRole role = UserRole.User)
        => new($"user-{Guid.NewGuid():N}", new string('A', 64), role);
}
