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

public class BookingServiceOverbookingTests
{
    private readonly Mock<IEventRepository> _eventRepositoryMock = new();
    private readonly Mock<IBookingRepository> _bookingRepositoryMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly BookingService _service;
    private readonly object _lock = new();

    public BookingServiceOverbookingTests()
    {
        var timeProvider = new FakeTimeProvider();
        timeProvider.SetUtcNow(TestHelper.Now);
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.Events).Returns(_eventRepositoryMock.Object);
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.Bookings).Returns(_bookingRepositoryMock.Object);
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.Users).Returns(_userRepositoryMock.Object);
        _service = new BookingService(_unitOfWork.Object, Mock.Of<ILogger<BookingService>>(), timeProvider);
    }

    [Fact]
    public async Task CreateBookingAsync_ConcurrentRequests_RespectsAvailableSeats()
    {
        var user = CreateUser();
        var @event = new Event("title", "description", TestHelper.Tomorrow, TestHelper.Tomorrow.AddHours(2), 5);
        _userRepositoryMock.Setup(repository => repository.GetByIdAsync(user.Id, TestContext.Current.CancellationToken)).ReturnsAsync(user);
        _eventRepositoryMock.Setup(repository => repository.GetByIdAsync(@event.Id, TestContext.Current.CancellationToken)).ReturnsAsync(@event);
        _bookingRepositoryMock.Setup(repository => repository.CountActiveByUserIdAsync(user.Id, TestContext.Current.CancellationToken)).ReturnsAsync(0);

        var successBookings = 0;
        var failedBookings = 0;
        var tasks = Enumerable.Range(0, 10).Select(async _ =>
        {
            try
            {
                await _service.CreateBookingAsync(@event.Id, user.Id, TestContext.Current.CancellationToken);
                lock (_lock) successBookings++;
            }
            catch (NoAvailableSeatsException)
            {
                lock (_lock) failedBookings++;
            }
        });

        await Task.WhenAll(tasks);

        Assert.Equal(5, successBookings);
        Assert.Equal(5, failedBookings);
        Assert.Equal(0, @event.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_ConcurrentRequests_CreatesUniqueBookingIds()
    {
        var user = CreateUser();
        var @event = new Event("title", "description", TestHelper.Tomorrow, TestHelper.Tomorrow.AddHours(2), 10);
        var ids = new HashSet<Guid>();
        _userRepositoryMock.Setup(repository => repository.GetByIdAsync(user.Id, TestContext.Current.CancellationToken)).ReturnsAsync(user);
        _eventRepositoryMock.Setup(repository => repository.GetByIdAsync(@event.Id, TestContext.Current.CancellationToken)).ReturnsAsync(@event);
        _bookingRepositoryMock.Setup(repository => repository.CountActiveByUserIdAsync(user.Id, TestContext.Current.CancellationToken)).ReturnsAsync(0);

        var tasks = Enumerable.Range(0, 10).Select(async _ =>
        {
            var result = await _service.CreateBookingAsync(@event.Id, user.Id, TestContext.Current.CancellationToken);
            lock (_lock) Assert.True(ids.Add(result.Id));
        });

        await Task.WhenAll(tasks);

        Assert.Equal(10, ids.Count);
    }

    private static User CreateUser()
        => new($"user-{Guid.NewGuid():N}", new string('A', 64), UserRole.User);
}
