using BGA.API.Application.Exceptions;
using BGA.API.Application.Models;
using BGA.API.Application.Services.Implementations;
using BGA.API.Infrastructure.DataAccess;
using BGA.API.Infrastructure.DataAccess.Repositories.Interfaces;
using BGA.API.Infrastructure.Models;
using BGA.API.Tests.Helpers;
using Moq;

namespace BGA.API.Tests;

public class EventServiceTests
{
    private readonly Mock<IEventRepository> _eventRepositoryMock;
    private readonly Mock<IBookingRepository> _bookingRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly EventService _service;

    public EventServiceTests()
    {
        _eventRepositoryMock = new Mock<IEventRepository>();
        _bookingRepositoryMock = new Mock<IBookingRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _unitOfWork.Setup(u => u.Events).Returns(_eventRepositoryMock.Object);
        _unitOfWork.Setup(u => u.Bookings).Returns(_bookingRepositoryMock.Object);
        _service = new EventService(_unitOfWork.Object);
    }

    [Fact]
    public async Task GetAllAsync_WithPageAndPageSize_ReturnsPaginatedResultWithEvents()
    {
        // Arrange
        var page = 1;
        var pageSize = 10;
        var totalItems = 12;
        var events = CreateEvents(count: totalItems);

        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Events.GetAll(null, null, null))
            .Returns(events);

        // Act
        var result = await _service.GetAllAsync(title: null, from: null, to: null, page: page, pageSize: pageSize, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<PaginatedResult<Event>>(result);
        Assert.NotNull(result);
        Assert.Equal(totalItems, result.TotalItems);
        Assert.Equal(page, result.PageNumber);
        Assert.Equal(pageSize, result.PageSize);
        Assert.Equal(pageSize, result.Items.Count());

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.GetAll(null, null, null), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithPageLessThanOne_ThrowValidationException()
    {
        // Arrange & Act
        var exception = await Assert.ThrowsAsync<ValidationException>(
            async () => await _service.GetAllAsync(null, null, null, 0, 10, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.HasSingleError("page", "page can be more or equal than 1");

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.GetAll(It.IsAny<string?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>()), Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_WithPageSizeLessOrEqualThanZero_ThrowValidationException()
    {
        // Arrange & Act
        var exception = await Assert.ThrowsAsync<ValidationException>(
            async () => await _service.GetAllAsync(null, null, null, 1, -1, cancellationToken: TestContext.Current.CancellationToken));

        exception.HasSingleError("pageSize", "pageSize can be more or equal than 0");

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.GetAll(It.IsAny<string?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>()), Times.Never);
    }

    public static IEnumerable<object?[]> DifferentDates()
    {
        return
        [
            [ null,                                                                        null,                                                             ],
            [ null,                                                                        new DateTimeOffset(2026, 03, 28, 0, 0, 0, TimeSpan.Zero)  ],
            [ new DateTimeOffset(2026, 03, 25, 0, 0, 0, TimeSpan.Zero),            null,                                                             ],
            [ new DateTimeOffset(2026, 03, 26, 0, 0, 0, TimeSpan.Zero),            new DateTimeOffset(2026, 03, 26, 0, 0, 0, TimeSpan.Zero)  ],
            [ new DateTimeOffset(2026, 03, 26, 0, 0, 0, TimeSpan.Zero),            new DateTimeOffset(2026, 03, 27, 0, 0, 0, TimeSpan.Zero)  ],
            [ new DateTimeOffset(2026, 03, 30, 10, 0, 0, TimeSpan.Zero),           new DateTimeOffset(2026, 03, 30, 10, 0, 1, TimeSpan.Zero) ]
        ];
    }

    [Theory]
    [MemberData(nameof(DifferentDates))]
    public async Task GetAllAsync_WithDifferentWaysForFromAndTo_ReturnsPaginatedResultWithEvents(DateTimeOffset? from, DateTimeOffset? to)
    {
        // Arrange
        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Events.GetAll(null, from, to))
            .Returns(Enumerable.Empty<Event>().AsQueryable());

        // Act
        var result = await _service.GetAllAsync(null, from, to, 1, 10, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<PaginatedResult<Event>>(result);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.GetAll(null, from, to), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithFromMoreThanTo_ThrowValidationException()
    {
        // Arrange & Act
        var exception = await Assert.ThrowsAsync<ValidationException>(
            async () => await _service.GetAllAsync(null, new DateTimeOffset(2026, 01, 30, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 01, 29, 0, 0, 0, TimeSpan.Zero), 1, 10, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.HasSingleError("to", "to can be more or equal than from");

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.GetAll(It.IsAny<string?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>()), Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_WithPageLessThanOneAndPageSizeLessThanZeroAndToMoreThanFrom_ThrowValidationException()
    {
        // Arrange & Act
        var exception = await Assert.ThrowsAsync<ValidationException>(
            async () => await _service.GetAllAsync(null, new DateTimeOffset(2026, 01, 30, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 01, 29, 0, 0, 0, TimeSpan.Zero), -1, -1, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.HasSingleError("page", "page can be more or equal than 1");

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.GetAll(It.IsAny<string?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>()), Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_WithRepositoryThrowsException()
    {
        // Arrange
        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Events.GetAll(null, null, null))
            .Throws(new Exception());

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            async () => await _service.GetAllAsync(null, null, null, 1, 10, cancellationToken: TestContext.Current.CancellationToken));

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.GetAll(null, null, null), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithCorrectId_ReturnsEvent()
    {
        // Arrange
        var id = Guid.NewGuid();
        var @event = new Event("Jumping", "Jumping with other beautiful women", new DateTimeOffset(2026, 03, 26, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 03, 27, 0, 0, 0, TimeSpan.Zero), int.MaxValue);

        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Events.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(@event);

        // Act
        var result = await _service.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<Event>(result);
        Assert.Equal(@event, result);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithRepositoryThrowException()
    {
        // Arrange
        var id = Guid.NewGuid();

        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Events.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken))
            .ThrowsAsync(new KeyNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await _service.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken));

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithNotExistsEvent_ThrowNotFoundException()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Events.GetByIdAsync(eventId, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync((Event)null!);

        // Act
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            async () => await _service.GetByIdAsync(eventId, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal("Event not found", exception.Message);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.GetByIdAsync(eventId, cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithValidEvent_ReturnsEvent()
    {
        // Arrange
        var @event = new Event("Cycling", "Cycling with other crazy people", new DateTimeOffset(2026, 05, 25, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 05, 29, 0, 0, 0, TimeSpan.Zero), int.MaxValue);

        // Act
        var result = await _service.CreateAsync(@event, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<Event>(result);
        Assert.NotNull(result);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.CreateAsync(It.IsAny<Event>(), cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithRepositoryThrowsException()
    {
        // Arrange
        var @event = new Event("Cycling", "Cycling with other crazy people", new DateTimeOffset(2026, 05, 25, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 05, 29, 0, 0, 0, TimeSpan.Zero), int.MaxValue);

        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Events.CreateAsync(@event, cancellationToken: TestContext.Current.CancellationToken))
            .ThrowsAsync(new Exception());

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            async () => await _service.CreateAsync(@event, cancellationToken: TestContext.Current.CancellationToken));

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.CreateAsync(@event, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithValidEvent()
    {
        // Arrange
        var id = Guid.NewGuid();

        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Events.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(CreateEvent());

        // Act
        await _service.UpdateAsync(id, "Jumping Girls", "Jumping girls with other beautiful women", new DateTimeOffset(2026, 03, 26, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 03, 27, 0, 0, 0, TimeSpan.Zero), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.Update(It.IsAny<Event>()), Times.Once);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNotExistsEvent_ThrowNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();

        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Events.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync((Event)null!);

        // Act
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            async () => await _service.UpdateAsync(id, "Jumping Girls", "Jumping girls with other beautiful women", new DateTimeOffset(2026, 03, 26, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 03, 27, 0, 0, 0, TimeSpan.Zero), cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal("Event not found", exception.Message);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.Update(It.IsAny<Event>()), Times.Never);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithRepositoryException()
    {
        // Arrange
        var id = Guid.NewGuid();

        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Events.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(CreateEvent());

        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Events.Update(It.IsAny<Event>()))
            .Throws(new KeyNotFoundException());

        // Act & Assert        
        await Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await _service.UpdateAsync(id, "Jumping Girls", "Jumping girls with other beautiful women", new DateTimeOffset(2026, 03, 26, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 03, 27, 0, 0, 0, TimeSpan.Zero), cancellationToken: TestContext.Current.CancellationToken));

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.Update(It.IsAny<Event>()), Times.Once);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Never);
    }

    [Fact]
    public async Task RemoveAsync_WithValidId()
    {
        // Arrange
        var id = Guid.NewGuid();

        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Events.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(CreateEvent());

        // Act
        await _service.RemoveAsync(id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.Remove(It.IsAny<Event>()), Times.Once);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_WhenEventNotFound_ThrowNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();

        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Events.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync((Event)null!);

        // Act        
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            async () => await _service.RemoveAsync(id, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal("Event not found", exception.Message);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.Remove(It.IsAny<Event>()), Times.Never);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Never);
    }

    [Fact]
    public async Task RemoveAsync_WithRepositoryThrowsException()
    {
        // Arrange
        var id = Guid.NewGuid();

        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Events.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(CreateEvent());

        _unitOfWork
            .Setup(unitOfWork => unitOfWork.Events.Remove(It.IsAny<Event>()))
            .Throws(new Exception());

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            async () => await _service.RemoveAsync(id, cancellationToken: TestContext.Current.CancellationToken));

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.Events.Remove(It.IsAny<Event>()), Times.Once);

        _unitOfWork
            .Verify(unitOfWork => unitOfWork.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Never);
    }

    private static Event CreateEvent()
    {
        return CreateEvents(1).First();
    }

    private static IQueryable<Event> CreateEvents(int count, List<string>? titles = null, List<DateTimeOffset>? startAtDates = null, List<DateTimeOffset>? endAtDates = null)
    {
        var list = new List<Event>();
        for (int i = 0; i < count; i++)
        {
            list.Add(
                new Event(
                    title: titles != null ? titles[i] : i.ToString(),
                    description: null,
                    startAt: startAtDates != null ? startAtDates[i] : (endAtDates != null ? endAtDates[i].AddDays(-1) : TestHelper.Yesterday),
                    endAt: endAtDates != null ? endAtDates[i] : (startAtDates != null ? startAtDates[i].AddDays(1) : TestHelper.Tomorrow),
                    totalSeats: int.MaxValue
                )
            );
        }

        return list.AsQueryable();
    }
}
