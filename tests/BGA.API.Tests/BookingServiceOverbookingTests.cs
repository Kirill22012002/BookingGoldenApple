using BGA.API.Application.Services.Implementations;
using BGA.API.Infrastructure.Models;
using BGA.API.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace BGA.API.Tests;

public class BookingServiceOverbookingTests
{
    private readonly Mock<IBookingRepository> _bookingRepository;
    private readonly Mock<IEventRepository> _eventRepository;
    private readonly Mock<ILogger<BookingService>> _logger;
    private readonly FakeTimeProvider _timeProvider;
    private readonly BookingService _service;

    private readonly object _lock = new();

    public BookingServiceOverbookingTests()
    {
        _bookingRepository = new Mock<IBookingRepository>();
        _eventRepository = new Mock<IEventRepository>();
        _logger = new Mock<ILogger<BookingService>>();
        _timeProvider = new FakeTimeProvider();
        _service = new BookingService(_bookingRepository.Object, _eventRepository.Object, _logger.Object, _timeProvider);
    }

    [Theory]
    [InlineData(5, 20, 5, 15, 0)]
    [InlineData(2, 2, 2, 0, 0)]
    [InlineData(1, 2, 1, 1, 0)]
    [InlineData(2, 3, 2, 1, 0)]
    [InlineData(3, 2, 2, 0, 1)]
    [InlineData(0, 2, 0, 2, 0)]
    [InlineData(100, 50, 50, 0, 50)]
    [InlineData(1, 100, 1, 99, 0)]
    [InlineData(10, 0, 0, 0, 10)]
    [InlineData(5, 1, 1, 0, 4)]
    [InlineData(7, 7, 7, 0, 0)]
    [InlineData(0, 0, 0, 0, 0)]
    public async Task CreateBookingAsync_OverbookingTests(int initialSeats, int concurrentRequests, int expectedSuccessBookings, int expectedFailedBookings, int expectedAvailableSeats)
    {
        // Arrange
        int successBookings = 0;
        int failedBookings = 0;

        var @event = new Event("title", "description", DateTimeOffset.MinValue, DateTimeOffset.MaxValue, initialSeats);

        _eventRepository
            .Setup(repository => repository.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(@event);

        _bookingRepository
            .Setup(repository => repository.CreateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(true);

        // Act
        var tasks = Enumerable
            .Range(0, concurrentRequests)
            .Select(async i =>
            {
                var result = await _service.CreateBookingAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken);
                lock (_lock)
                {
                    if (result.Succeeded) successBookings++;
                    else failedBookings++;
                }

                return result;
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
        var @event = new Event("title", "description", DateTimeOffset.MinValue, DateTimeOffset.MaxValue, concurrentRequests);
        _eventRepository
            .Setup(repository => repository.GetByIdAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(@event);

        _bookingRepository
            .Setup(repository => repository.CreateAsync(It.IsAny<Booking>(), cancellationToken: TestContext.Current.CancellationToken))
            .Callback<Booking, CancellationToken>((booking, cancellationToken) =>
            {
                booking.Id = Guid.NewGuid();
            })
            .ReturnsAsync(true);

        // Act
        var tasks = Enumerable
            .Range(0, concurrentRequests)
            .Select(async i =>
            {
                var result = await _service.CreateBookingAsync(@event.Id, cancellationToken: TestContext.Current.CancellationToken);
                lock (_lock)
                {
                    if (result.Data != null)
                        Assert.True(ids.Add(result.Data.Id));
                }

                return result;
            });

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(concurrentRequests, ids.Count);
    }
}
