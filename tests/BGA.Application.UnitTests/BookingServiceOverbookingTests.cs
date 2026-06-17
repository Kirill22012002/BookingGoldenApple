using BGA.Application.UnitTests.Helpers;
using BGA.Application.Repositories;
using BGA.Application.Services.Implementations;
using BGA.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;
using BGA.Domain.Exceptions;

namespace BGA.Application.UnitTests;

public class BookingServiceOverbookingTests
{
    private readonly Mock<IEventRepository> _eventRepositoryMock;
    private readonly Mock<IBookingRepository> _bookingRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ILogger<BookingService>> _logger;
    private readonly FakeTimeProvider _timeProvider;
    private readonly BookingService _service;

    private readonly object _lock = new();

    public BookingServiceOverbookingTests()
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

    [Theory]
    [InlineData(5, 20, 5, 15, 0)]
    [InlineData(2, 2, 2, 0, 0)]
    [InlineData(1, 2, 1, 1, 0)]
    [InlineData(2, 3, 2, 1, 0)]
    [InlineData(3, 2, 2, 0, 1)]
    [InlineData(100, 50, 50, 0, 50)]
    [InlineData(1, 100, 1, 99, 0)]
    [InlineData(10, 0, 0, 0, 10)]
    [InlineData(5, 1, 1, 0, 4)]
    [InlineData(7, 7, 7, 0, 0)]
    public async Task CreateBookingAsync_OverbookingTests(int initialSeats, int concurrentRequests, int expectedSuccessBookings, int expectedFailedBookings, int expectedAvailableSeats)
    {
        // Arrange
        int successBookings = 0;
        int failedBookings = 0;

        var @event = new Event("title", "description", TestHelper.Yesterday, TestHelper.Tomorrow, initialSeats);

        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Events.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(@event);

        // Act
        var tasks = Enumerable
            .Range(0, concurrentRequests)
            .Select(async i =>
            {
                try
                {
                    await _service.CreateBookingAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken);
                    lock (_lock) successBookings++;
                }
                catch (NoAvailableSeatsException)
                {
                    lock (_lock) failedBookings++;
                }
            });

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(expectedSuccessBookings, successBookings);
        Assert.Equal(expectedFailedBookings, failedBookings);
        Assert.Equal(expectedAvailableSeats, @event.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_AllBookingsCreateWithUniqueId()
    {
        // Arrange
        var concurrentRequests = 10;
        HashSet<Guid> ids = [];
        var @event = new Event("title", "description", TestHelper.Yesterday, TestHelper.Tomorrow, concurrentRequests);
        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Events.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(@event);

        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Bookings.CreateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken));

        // Act
        var tasks = Enumerable
            .Range(0, concurrentRequests)
            .Select(async i =>
            {
                var result = await _service.CreateBookingAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken);
                lock (_lock)
                {
                    if (result != null)
                        Assert.True(ids.Add(result.Id));
                }

                return result;
            });

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(concurrentRequests, ids.Count);
    }
}
