using BGA.Events.Application.Repositories;
using BGA.Events.Application.Services.Implementations;
using BGA.Events.Application.UnitTests.Helpers;
using BGA.Events.Domain.Exceptions;
using BGA.Events.Domain.Models;
using Moq;

namespace BGA.Events.Application.UnitTests;

public class EventServiceTests
{
    private readonly Mock<IEventRepository> _eventRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly EventService _service;

    public EventServiceTests()
    {
        _eventRepositoryMock = new Mock<IEventRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _unitOfWork.Setup(unitOfWork => unitOfWork.Events).Returns(_eventRepositoryMock.Object);
        _service = new EventService(_unitOfWork.Object);
    }

    [Fact]
    public async Task GetAllAsync_WithPageAndPageSize_ReturnsPaginatedResultWithEvents()
    {
        var page = 1;
        var pageSize = 10;
        var totalItems = 12;
        var events = CreateEvents(totalItems);

        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Events.GetAll(null, null, null))
            .Returns(events);

        var result = await _service.GetAllAsync(null, null, null, page, pageSize, TestContext.Current.CancellationToken);

        Assert.Equal(totalItems, result.TotalItems);
        Assert.Equal(page, result.PageNumber);
        Assert.Equal(pageSize, result.PageSize);
        Assert.Equal(pageSize, result.Items.Count());
    }

    [Fact]
    public async Task GetAllAsync_WithPageLessThanOne_ThrowValidationException()
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.GetAllAsync(null, null, null, 0, 10, TestContext.Current.CancellationToken));

        exception.HasSingleError("page", "page can be more or equal than 1");
    }

    [Fact]
    public async Task GetAllAsync_WithPageSizeLessOrEqualThanZero_ThrowValidationException()
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.GetAllAsync(null, null, null, 1, -1, TestContext.Current.CancellationToken));

        exception.HasSingleError("pageSize", "pageSize can be more or equal than 0");
    }

    [Theory]
    [MemberData(nameof(DifferentDates))]
    public async Task GetAllAsync_WithDifferentWaysForFromAndTo_ReturnsPaginatedResultWithEvents(DateTimeOffset? from, DateTimeOffset? to)
    {
        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Events.GetAll(null, from, to))
            .Returns(Enumerable.Empty<Event>().AsQueryable());

        var result = await _service.GetAllAsync(null, from, to, 1, 10, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        _unitOfWork.Verify(unitOfWork => unitOfWork.Events.GetAll(null, from, to), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithFromMoreThanTo_ThrowValidationException()
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.GetAllAsync(
                null,
                new DateTimeOffset(2026, 1, 30, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 1, 29, 0, 0, 0, TimeSpan.Zero),
                1,
                10,
                TestContext.Current.CancellationToken));

        exception.HasSingleError("to", "to can be more or equal than from");
    }

    [Fact]
    public async Task GetByIdAsync_WithCorrectId_ReturnsEvent()
    {
        var id = Guid.NewGuid();
        var @event = new Event("Jumping", "Jumping with other beautiful women", TestHelper.Yesterday, TestHelper.Tomorrow, int.MaxValue);

        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Events.GetByIdAsync(id, TestContext.Current.CancellationToken))
            .ReturnsAsync(@event);

        var result = await _service.GetByIdAsync(id, TestContext.Current.CancellationToken);

        Assert.Equal(@event, result);
    }

    [Fact]
    public async Task GetByIdAsync_WithNotExistsEvent_ThrowNotFoundException()
    {
        var id = Guid.NewGuid();

        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Events.GetByIdAsync(id, TestContext.Current.CancellationToken))
            .ReturnsAsync((Event)null!);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.GetByIdAsync(id, TestContext.Current.CancellationToken));

        Assert.Equal("Event not found", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_WithValidEvent_ReturnsEvent()
    {
        var @event = new Event("Cycling", "Cycling with other crazy people", TestHelper.Yesterday, TestHelper.Tomorrow, int.MaxValue);

        var result = await _service.CreateAsync(@event, TestContext.Current.CancellationToken);

        Assert.Equal(@event, result);
        _unitOfWork.Verify(unitOfWork => unitOfWork.Events.CreateAsync(@event, TestContext.Current.CancellationToken), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task TryReserveSeatsAsync_WhenSeatsAvailable_ReservesSeats()
    {
        var id = Guid.NewGuid();
        var @event = new Event("Cycling", null, TestHelper.Yesterday, TestHelper.Tomorrow, 5);
        _unitOfWork.Setup(unitOfWork => unitOfWork.Events.GetByIdAsync(id, TestContext.Current.CancellationToken)).ReturnsAsync(@event);

        var result = await _service.TryReserveSeatsAsync(id, 2, TestContext.Current.CancellationToken);

        Assert.True(result);
        Assert.Equal(3, @event.AvailableSeats);
        _unitOfWork.Verify(unitOfWork => unitOfWork.Events.Update(@event), Times.Once);
    }

    [Fact]
    public async Task TryReserveSeatsAsync_WhenEventMissing_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _unitOfWork.Setup(unitOfWork => unitOfWork.Events.GetByIdAsync(id, TestContext.Current.CancellationToken)).ReturnsAsync((Event)null!);
        await Assert.ThrowsAsync<NotFoundException>(() => _service.TryReserveSeatsAsync(id, 1, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task TryReserveSeatsAsync_WhenSeatsUnavailable_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        var @event = new Event("Cycling", null, TestHelper.Yesterday, TestHelper.Tomorrow, 1);
        _unitOfWork.Setup(unitOfWork => unitOfWork.Events.GetByIdAsync(id, TestContext.Current.CancellationToken)).ReturnsAsync(@event);
        var result = await _service.TryReserveSeatsAsync(id, 2, TestContext.Current.CancellationToken);
        Assert.False(result);
    }

    [Fact]
    public async Task UpdateAsync_WithValidEvent()
    {
        var id = Guid.NewGuid();

        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Events.GetByIdAsync(id, TestContext.Current.CancellationToken))
            .ReturnsAsync(CreateEvents(1).Single());

        await _service.UpdateAsync(id, "Updated", "Updated description", TestHelper.Now, TestHelper.Tomorrow, TestContext.Current.CancellationToken);

        _unitOfWork.Verify(unitOfWork => unitOfWork.Events.Update(It.IsAny<Event>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_WithValidId()
    {
        var id = Guid.NewGuid();

        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Events.GetByIdAsync(id, TestContext.Current.CancellationToken))
            .ReturnsAsync(CreateEvents(1).Single());

        await _service.RemoveAsync(id, TestContext.Current.CancellationToken);

        _unitOfWork.Verify(unitOfWork => unitOfWork.Events.Remove(It.IsAny<Event>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken), Times.Once);
    }

    public static IEnumerable<object?[]> DifferentDates()
    {
        return
        [
            [null, null],
            [null, new DateTimeOffset(2026, 3, 28, 0, 0, 0, TimeSpan.Zero)],
            [new DateTimeOffset(2026, 3, 25, 0, 0, 0, TimeSpan.Zero), null],
            [new DateTimeOffset(2026, 3, 26, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 3, 26, 0, 0, 0, TimeSpan.Zero)],
            [new DateTimeOffset(2026, 3, 26, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 3, 27, 0, 0, 0, TimeSpan.Zero)]
        ];
    }

    private static IQueryable<Event> CreateEvents(int count)
    {
        return Enumerable.Range(0, count)
            .Select(index => new Event(
                index.ToString(),
                null,
                TestHelper.Yesterday.AddDays(index),
                TestHelper.Tomorrow.AddDays(index),
                int.MaxValue))
            .AsQueryable();
    }
}
