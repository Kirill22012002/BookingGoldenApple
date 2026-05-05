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
    private readonly Mock<IEventRepository> _repository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly EventService _service;

    public EventServiceTests()
    {
        _repository = new Mock<IEventRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _service = new EventService(_repository.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task GetAllAsync_WithPageAndPageSize_ReturnsPaginatedResultWithEvents()
    {
        // Arrange
        var page = 1;
        var pageSize = 10;
        var totalItems = 12;
        var events = CreateEvents(count: totalItems);

        _repository
            .Setup(repository => repository.GetAllAsync(cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(events);

        // Act
        var result = await _service.GetAllAsync(title: null, from: null, to: null, page: page, pageSize: pageSize, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<PaginatedResult<Event>>(result);
        Assert.NotNull(result);
        Assert.Equal(totalItems, result.TotalItems);
        Assert.Equal(page, result.PageNumber);
        Assert.Equal(pageSize, result.PageSize);
        Assert.Equal(pageSize, result.Items.Count());

        _repository
            .Verify(repository => repository.GetAllAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithFilterByTitle_ReturnsPaginatedResultWithEvents()
    {
        // Arrange
        var searchSubstring = "ing";
        var titles = new List<string> { "Jogging", "Running", "Theathre", "JUMPING", "Basketball" };
        var expectedTitles = new List<string> { "Jogging", "Running", "JUMPING" };
        var notExpectedTitle = "Theathre";
        var events = CreateEvents(count: titles.Count, titles: titles);

        _repository
            .Setup(repository => repository.GetAllAsync(cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(events);

        // Act
        var result = await _service.GetAllAsync(title: searchSubstring, from: null, to: null, page: 1, pageSize: 10, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<PaginatedResult<Event>>(result);
        Assert.NotNull(result);
        Assert.Equal(expectedTitles.Count, result.Items.Count());
        Assert.All(result.Items, @event => expectedTitles.Contains(@event.Title));
        Assert.DoesNotContain(notExpectedTitle, result.Items.Select(@event => @event.Title));

        _repository
            .Verify(repository => repository.GetAllAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithFilterByStartAt_ReturnsPaginatedResultWithEvents()
    {
        // Arrange
        var searchStartAt = new DateTimeOffset(2026, 03, 15, 0, 0, 0, TimeSpan.FromHours(0));
        var startAtDates = new List<DateTimeOffset> { new(2026, 03, 14, 0, 0, 0, TimeSpan.FromHours(0)), new(2026, 03, 15, 0, 0, 0, TimeSpan.FromHours(0)), new(2026, 03, 16, 0, 0, 0, TimeSpan.FromHours(0)) };
        var expectedStartAtDates = new List<DateTimeOffset> { new(2026, 03, 15, 0, 0, 0, TimeSpan.FromHours(0)), new(2026, 03, 16, 0, 0, 0, TimeSpan.FromHours(0)) };
        var notExpectedStartAtDate = new DateTimeOffset(2026, 03, 14, 0, 0, 0, TimeSpan.FromHours(0));
        var events = CreateEvents(count: startAtDates.Count, startAtDates: startAtDates);

        _repository
            .Setup(repository => repository.GetAllAsync(cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(events);

        // Act
        var result = await _service.GetAllAsync(title: null, from: searchStartAt, to: null, page: 1, pageSize: 10, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<PaginatedResult<Event>>(result);
        Assert.NotNull(result);
        Assert.Equal(expectedStartAtDates.Count, result.Items.Count());
        Assert.All(result.Items, @event => expectedStartAtDates.Contains(@event.StartAt));
        Assert.DoesNotContain(notExpectedStartAtDate, result.Items.Select(@event => @event.StartAt));

        _repository
            .Verify(repository => repository.GetAllAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithFilterByEndAt_ReturnsPaginatedResultWithEvents()
    {
        // Arrange
        var searchEndAt = new DateTimeOffset(2026, 03, 15, 0, 0, 0, TimeSpan.FromHours(0));
        var endAtDates = new List<DateTimeOffset> { new(2026, 03, 14, 0, 0, 0, TimeSpan.FromHours(0)), new(2026, 03, 16, 0, 0, 0, TimeSpan.FromHours(0)), new(2026, 03, 17, 0, 0, 0, TimeSpan.FromHours(0)) };
        var expectedEndAtDate = new DateTimeOffset(2026, 03, 14, 0, 0, 0, TimeSpan.FromHours(0));
        var notExpectedEndDate = new DateTimeOffset(2026, 03, 16, 0, 0, 0, TimeSpan.FromHours(0));
        var events = CreateEvents(count: endAtDates.Count, endAtDates: endAtDates);

        _repository
            .Setup(repository => repository.GetAllAsync(cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(events);

        // Act
        var result = await _service.GetAllAsync(title: null, from: null, to: searchEndAt, page: 1, pageSize: 10, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<PaginatedResult<Event>>(result);
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Contains(expectedEndAtDate, result.Items.Select(@event => @event.EndAt));
        Assert.DoesNotContain(notExpectedEndDate, result.Items.Select(@event => @event.EndAt));

        _repository
            .Verify(repository => repository.GetAllAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithFilterBothStartAtAndEndAt_ReturnsPaginatedResultWithEvents()
    {
        // Arrange
        //  14, (15,  16, 17) 
        //       ||
        // (24,  25), 26, 27
        // Only item with StartAt: 15 and EndAt: 25 will be in result
        var searchStartAt = new DateTimeOffset(2026, 03, 15, 0, 0, 0, TimeSpan.FromHours(0));
        var searchEndAt = new DateTimeOffset(2026, 03, 25, 0, 0, 0, TimeSpan.FromHours(0));
        var startAtDates = new List<DateTimeOffset> { new(2026, 03, 14, 0, 0, 0, TimeSpan.FromHours(0)), new(2026, 03, 15, 0, 0, 0, TimeSpan.FromHours(0)), new(2026, 03, 16, 0, 0, 0, TimeSpan.FromHours(0)), new(2026, 03, 17, 0, 0, 0, TimeSpan.FromHours(0)) };
        var endAtDates = new List<DateTimeOffset> { new(2026, 03, 24, 0, 0, 0, TimeSpan.FromHours(0)), new(2026, 03, 25, 0, 0, 0, TimeSpan.FromHours(0)), new(2026, 03, 26, 0, 0, 0, TimeSpan.FromHours(0)), new(2026, 03, 27, 0, 0, 0, TimeSpan.FromHours(0)) };
        var expectedStartAtDate = new DateTimeOffset(2026, 03, 15, 0, 0, 0, TimeSpan.FromHours(0));
        var expectedEndAtDate = new DateTimeOffset(2026, 03, 25, 0, 0, 0, TimeSpan.FromHours(0));
        var notExpectedStartAtDate = new DateTimeOffset(2026, 03, 14, 0, 0, 0, TimeSpan.FromHours(0));
        var notExpectedEndAtDate = new DateTimeOffset(2026, 03, 26, 0, 0, 0, TimeSpan.FromHours(0));

        var events = CreateEvents(count: startAtDates.Count, startAtDates: startAtDates, endAtDates: endAtDates);

        _repository
            .Setup(repository => repository.GetAllAsync(cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(events);

        // Act
        var result = await _service.GetAllAsync(title: null, from: searchStartAt, to: searchEndAt, page: 1, pageSize: 10, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<PaginatedResult<Event>>(result);
        Assert.NotNull(result);
        Assert.Single(result.Items, @event => @event.StartAt == expectedStartAtDate && @event.EndAt == expectedEndAtDate);
        Assert.DoesNotContain(notExpectedStartAtDate, result.Items.Select(@event => @event.StartAt));
        Assert.DoesNotContain(notExpectedEndAtDate, result.Items.Select(@event => @event.EndAt));

        _repository
            .Verify(repository => repository.GetAllAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    public static IEnumerable<object[]> MultipleFilters()
    {
        return
        [
            [ "ing",        "2026-03-26T00:00:00-00:00",   "2026-03-27T00:00:00-00:00",   true  ],
            [ "ing",        "2026-03-26T00:00:00-00:00",   "2026-03-28T00:00:00-00:00",   true  ],
            [ "ing",        "2026-03-25T00:00:00-00:00",   "2026-03-26T00:00:00-00:00",   true  ],
            [ "ing",        "2026-03-26T00:00:00-00:00",   "2026-03-26T00:00:00-00:00",   false ],
            [ "jogging",    "2026-03-26T00:00:00-00:00",   "2026-03-27T00:00:00-00:00",   true  ],
            [ "JOGGING",    "2026-03-26T00:00:00-00:00",   "2026-03-27T00:00:00-00:00",   true  ],
            [ "yo",         "2026-03-26T00:00:00-00:00",   "2026-03-27T00:00:00-00:00",   true  ],
            [ "running",    "2026-03-27T00:00:00-00:00",   "2026-03-28T00:00:00-00:00",   true  ],
            [ "run",        "2026-03-27T00:00:00-00:00",   "2026-03-28T00:00:00-00:00",   true  ],
            [ "ing",        "2026-03-27T00:00:00-00:00",   "2026-03-28T00:00:00-00:00",   true  ],
            [ "theatre",    "2026-03-26T00:00:00-00:00",   "2026-03-28T00:00:00-00:00",   true  ],
            [ "ing",        "2026-03-28T00:00:00-00:00",   "2026-03-29T00:00:00-00:00",   false ],
            [ "jog",        "2026-03-26T00:00:00-00:00",   "2026-03-27T00:00:00-00:00",   true  ]
        ];
    }

    [Theory]
    [MemberData(nameof(MultipleFilters))]
    public async Task GetAllAsync_WithFilterAllTitleStartAtAndEndAt_ReturnsPaginatedResultWithEvents(string searchTitle, DateTimeOffset searchStartAt, DateTimeOffset searchEndAt, bool isInclude)
    {
        // Arrange
        var events = new List<Event>()
        {
            new("Jogging", null, new DateTimeOffset(2026, 03, 26, 0, 0, 0, TimeSpan.FromHours(0)), new DateTimeOffset(2026, 03, 27, 0, 0, 0, TimeSpan.FromHours(0)), int.MaxValue),
            new("Theatre", null, new DateTimeOffset(2026, 03, 26, 0, 0, 0, TimeSpan.FromHours(0)), new DateTimeOffset(2026, 03, 28, 0, 0, 0, TimeSpan.FromHours(0)), int.MaxValue),
            new("Morning jog", null, new DateTimeOffset(2026, 03, 25, 0, 0, 0, TimeSpan.FromHours(0)), new DateTimeOffset(2026, 03, 26, 0, 0, 0, TimeSpan.FromHours(0)), int.MaxValue),
            new("JOGGING", null, new DateTimeOffset(2026, 03, 26, 0, 0, 0, TimeSpan.FromHours(0)), new DateTimeOffset(2026, 03, 27, 0, 0, 0, TimeSpan.FromHours(0)), int.MaxValue),
            new("Jogging", null, new DateTimeOffset(2026, 03, 26, 0, 0, 0, TimeSpan.FromHours(0)), new DateTimeOffset(2026, 03, 28, 0, 0, 0, TimeSpan.FromHours(0)), int.MaxValue),
            new("Yoga", null, new DateTimeOffset(2026, 03, 26, 0, 0, 0, TimeSpan.FromHours(0)), new DateTimeOffset(2026, 03, 27, 0, 0, 0, TimeSpan.FromHours(0)), int.MaxValue),
            new("Running", null, new DateTimeOffset(2026, 03, 27, 0, 0, 0, TimeSpan.FromHours(0)), new DateTimeOffset(2026, 03, 28, 0, 0, 0, TimeSpan.FromHours(0)), int.MaxValue)
        };

        _repository
            .Setup(repository => repository.GetAllAsync(cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(events.AsQueryable());

        // Act
        var result = await _service.GetAllAsync(title: searchTitle, from: searchStartAt, to: searchEndAt, page: 1, pageSize: 10, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(isInclude, result.Items.Any(@event =>
            @event.Title.Contains(searchTitle, StringComparison.OrdinalIgnoreCase) &&
            @event.StartAt == searchStartAt &&
            @event.EndAt == searchEndAt));

        _repository
            .Verify(repository => repository.GetAllAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithPageLessThanOne_ThrowValidationException()
    {
        // Arrange & Act
        var exception = await Assert.ThrowsAsync<ValidationException>(
            async () => await _service.GetAllAsync(null, null, null, 0, 10, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.HasSingleError("page", "page can be more or equal than 1");

        _repository
            .Verify(repository => repository.GetAllAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_WithPageSizeLessOrEqualThanZero_ThrowValidationException()
    {
        // Arrange & Act
        var exception = await Assert.ThrowsAsync<ValidationException>(
            async () => await _service.GetAllAsync(null, null, null, 1, -1, cancellationToken: TestContext.Current.CancellationToken));

        exception.HasSingleError("pageSize", "pageSize can be more or equal than 0");

        _repository
            .Verify(repository => repository.GetAllAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Never);
    }

    public static IEnumerable<object?[]> DifferentDates()
    {
        return
        [
            [ null,                                                                        null,                                                             ],
            [ null,                                                                        new DateTimeOffset(2026, 03, 28, 0, 0, 0, TimeSpan.FromHours(0))  ],
            [ new DateTimeOffset(2026, 03, 25, 0, 0, 0, TimeSpan.FromHours(0)),            null,                                                             ],
            [ new DateTimeOffset(2026, 03, 26, 0, 0, 0, TimeSpan.FromHours(0)),            new DateTimeOffset(2026, 03, 26, 0, 0, 0, TimeSpan.FromHours(0))  ],
            [ new DateTimeOffset(2026, 03, 26, 0, 0, 0, TimeSpan.FromHours(0)),            new DateTimeOffset(2026, 03, 27, 0, 0, 0, TimeSpan.FromHours(0))  ],
            [ new DateTimeOffset(2026, 03, 30, 10, 0, 0, TimeSpan.FromHours(0)),           new DateTimeOffset(2026, 03, 30, 10, 0, 1, TimeSpan.FromHours(0)) ]
        ];
    }

    [Theory]
    [MemberData(nameof(DifferentDates))]
    public async Task GetAllAsync_WithDifferentWaysForFromAndTo_ReturnsPaginatedResultWithEvents(DateTimeOffset? from, DateTimeOffset? to)
    {
        // Arrange & Act
        var result = await _service.GetAllAsync(null, from, to, 1, 10, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<PaginatedResult<Event>>(result);
    }

    [Fact]
    public async Task GetAllAsync_WithFromMoreThanTo_ThrowValidationException()
    {
        // Arrange & Act
        var exception = await Assert.ThrowsAsync<ValidationException>(
            async () => await _service.GetAllAsync(null, new DateTimeOffset(2026, 01, 30, 0, 0, 0, TimeSpan.FromHours(0)), new DateTimeOffset(2026, 01, 29, 0, 0, 0, TimeSpan.FromHours(0)), 1, 10, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.HasSingleError("to", "to can be more or equal than from");

        _repository
            .Verify(repository => repository.GetAllAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_WithPageLessThanOneAndPageSizeLessThanZeroAndToMoreThanFrom_ThrowValidationException()
    {
        // Arrange & Act
        var exception = await Assert.ThrowsAsync<ValidationException>(
            async () => await _service.GetAllAsync(null, new DateTimeOffset(2026, 01, 30, 0, 0, 0, TimeSpan.FromHours(0)), new DateTimeOffset(2026, 01, 29, 0, 0, 0, TimeSpan.FromHours(0)), -1, -1, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.HasSingleError("page", "page can be more or equal than 1");

        _repository
            .Verify(repository => repository.GetAllAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_WithRepositoryThrowsException()
    {
        // Arrange
        _repository
            .Setup(repository => repository.GetAllAsync(cancellationToken: TestContext.Current.CancellationToken))
            .ThrowsAsync(new Exception());

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            async () => await _service.GetAllAsync(null, null, null, 1, 10, cancellationToken: TestContext.Current.CancellationToken));

        _repository
            .Verify(repository => repository.GetAllAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithCorrectId_ReturnsEvent()
    {
        // Arrange
        var id = Guid.NewGuid();
        var @event = new Event("Jumping", "Jumping with other beautiful women", new DateTimeOffset(2026, 03, 26, 0, 0, 0, TimeSpan.FromHours(0)), new DateTimeOffset(2026, 03, 27, 0, 0, 0, TimeSpan.FromHours(0)), int.MaxValue);

        _repository
            .Setup(repository => repository.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(@event);

        // Act
        var result = await _service.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<Event>(result);
        Assert.Equal(@event, result);

        _repository
            .Verify(repository => repository.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithRepositoryThrowException()
    {
        // Arrange
        var id = Guid.NewGuid();

        _repository
            .Setup(repository => repository.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken))
            .ThrowsAsync(new KeyNotFoundException());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await _service.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken));

        _repository
            .Verify(repository => repository.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithNotExistsEvent_ThrowNotFoundException()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _repository
            .Setup(repository => repository.GetByIdAsync(eventId, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync((Event)null!);

        // Act
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            async () => await _service.GetByIdAsync(eventId, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal("Event not found", exception.Message);

        _repository
            .Verify(repository => repository.GetByIdAsync(eventId, cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithValidEvent_ReturnsEvent()
    {
        // Arrange
        var @event = new Event("Cycling", "Cycling with other crazy people", new DateTimeOffset(2026, 05, 25, 0, 0, 0, TimeSpan.FromHours(0)), new DateTimeOffset(2026, 05, 29, 0, 0, 0, TimeSpan.FromHours(0)), int.MaxValue);

        // Act
        var result = await _service.CreateAsync(@event, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<Event>(result);
        Assert.NotNull(result);

        _repository
            .Verify(repository => repository.CreateAsync(It.IsAny<Event>(), cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithRepositoryThrowsException()
    {
        // Arrange
        var @event = new Event("Cycling", "Cycling with other crazy people", new DateTimeOffset(2026, 05, 25, 0, 0, 0, TimeSpan.FromHours(0)), new DateTimeOffset(2026, 05, 29, 0, 0, 0, TimeSpan.FromHours(0)), int.MaxValue);

        _repository
            .Setup(repository => repository.CreateAsync(@event, cancellationToken: TestContext.Current.CancellationToken))
            .ThrowsAsync(new Exception());

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            async () => await _service.CreateAsync(@event, cancellationToken: TestContext.Current.CancellationToken));

        _repository
            .Verify(repository => repository.CreateAsync(@event, cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithValidEvent()
    {
        // Arrange
        var id = Guid.NewGuid();

        _repository
            .Setup(repository => repository.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(CreateEvent());

        // Act
        await _service.UpdateAsync(id, "Jumping Girls", "Jumping girls with other beautiful women", new DateTimeOffset(2026, 03, 26, 0, 0, 0, TimeSpan.FromHours(0)), new DateTimeOffset(2026, 03, 27, 0, 0, 0, TimeSpan.FromHours(0)), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        _repository
            .Verify(repository => repository.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _repository
            .Verify(repository => repository.Update(It.IsAny<Event>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNotExistsEvent_ThrowNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();

        _repository
            .Setup(repository => repository.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync((Event)null!);

        // Act
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            async () => await _service.UpdateAsync(id, "Jumping Girls", "Jumping girls with other beautiful women", new DateTimeOffset(2026, 03, 26, 0, 0, 0, TimeSpan.FromHours(0)), new DateTimeOffset(2026, 03, 27, 0, 0, 0, TimeSpan.FromHours(0)), cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal("Event not found", exception.Message);

        _repository
            .Verify(repository => repository.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _repository
            .Verify(repository => repository.Update(It.IsAny<Event>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithRepositoryException()
    {
        // Arrange
        var id = Guid.NewGuid();

        _repository
            .Setup(repository => repository.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(CreateEvent());

        _repository
            .Setup(repository => repository.Update(It.IsAny<Event>()))
            .Throws(new KeyNotFoundException());

        // Act & Assert        
        await Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await _service.UpdateAsync(id, "Jumping Girls", "Jumping girls with other beautiful women", new DateTimeOffset(2026, 03, 26, 0, 0, 0, TimeSpan.FromHours(0)), new DateTimeOffset(2026, 03, 27, 0, 0, 0, TimeSpan.FromHours(0)), cancellationToken: TestContext.Current.CancellationToken));

        _repository
            .Verify(repository => repository.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _repository
            .Verify(repository => repository.Update(It.IsAny<Event>()), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_WithValidId()
    {
        // Arrange
        var id = Guid.NewGuid();

        _repository
            .Setup(repository => repository.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(CreateEvent());

        // Act
        await _service.RemoveAsync(id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        _repository
            .Verify(repository => repository.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _repository
            .Verify(repository => repository.Remove(It.IsAny<Event>()), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_WhenEventNotFound_ThrowNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();

        _repository
            .Setup(repository => repository.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync((Event)null!);

        // Act        
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            async () => await _service.RemoveAsync(id, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal("Event not found", exception.Message);

        _repository
            .Verify(repository => repository.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _repository
            .Verify(repository => repository.Remove(It.IsAny<Event>()), Times.Never);
    }

    [Fact]
    public async Task RemoveAsync_WithRepositoryThrowsException()
    {
        // Arrange
        var id = Guid.NewGuid();

        _repository
            .Setup(repository => repository.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(CreateEvent());

        _repository
            .Setup(repository => repository.Remove(It.IsAny<Event>()))
            .Throws(new Exception());

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            async () => await _service.RemoveAsync(id, cancellationToken: TestContext.Current.CancellationToken));

        _repository
            .Verify(repository => repository.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

        _repository
            .Verify(repository => repository.Remove(It.IsAny<Event>()), Times.Once);
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
