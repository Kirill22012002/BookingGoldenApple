using BGA.API.Application;
using BGA.API.Application.Services.Implementations;
using BGA.API.Infrastructure.Models;
using BGA.API.Infrastructure.Repositories.Interfaces;
using Moq;

namespace BGA.API.Tests;

public class EventServiceTests
{
    private readonly Mock<IEventRepository> _repository;
    private readonly EventService _service;

    public EventServiceTests()
    {
        _repository = new Mock<IEventRepository>();
        _service = new EventService(_repository.Object);
    }

    [Fact]
    public async Task GetAllAsync_WithPageAndPageSize_ReturnsServiceResponseWithSuccessAndCorrectCounts()
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
        Assert.IsType<ServiceResponse<PaginatedResult<Event>>>(result);
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);
        Assert.Equal(totalItems, result.Data.TotalItems);
        Assert.Equal(page, result.Data.PageNumber);
        Assert.Equal(pageSize, result.Data.PageSize);
        Assert.Equal(pageSize, result.Data.Items.Count());

        _repository
            .Verify(repository => repository.GetAllAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithFilterByTitle_ReturnsServiceResponseWithSuccessAndCorrectValues()
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
        Assert.IsType<ServiceResponse<PaginatedResult<Event>>>(result);
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);
        Assert.Equal(expectedTitles.Count, result.Data.Items.Count());
        Assert.All(result.Data.Items, @event => expectedTitles.Contains(@event.Title));
        Assert.DoesNotContain(notExpectedTitle, result.Data.Items.Select(@event => @event.Title));

        _repository
            .Verify(repository => repository.GetAllAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithFilterByStartAt_ReturnsServiceResponseWithSuccessAndCorrectValues()
    {
        // Arrange
        var searchStartAt = new DateTime(2026, 03, 15);
        var startAtDates = new List<DateTime> { new(2026, 03, 14), new(2026, 03, 15), new(2026, 03, 16) };
        var expectedStartAtDates = new List<DateTime> { new(2026, 03, 15), new(2026, 03, 16) };
        var notExpectedStartAtDate = new DateTime(2026, 03, 14);
        var events = CreateEvents(count: startAtDates.Count, startAtDates: startAtDates);

        _repository
            .Setup(repository => repository.GetAllAsync(cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(events);

        // Act
        var result = await _service.GetAllAsync(title: null, from: searchStartAt, to: null, page: 1, pageSize: 10, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<ServiceResponse<PaginatedResult<Event>>>(result);
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);
        Assert.Equal(expectedStartAtDates.Count, result.Data.Items.Count());
        Assert.All(result.Data.Items, @event => expectedStartAtDates.Contains(@event.StartAt));
        Assert.DoesNotContain(notExpectedStartAtDate, result.Data.Items.Select(@event => @event.StartAt));

        _repository
            .Verify(repository => repository.GetAllAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithFilterByEndAt_ReturnsServiceResponseWithSuccessAndCorrectValues()
    {
        // Arrange
        var searchEndAt = new DateTime(2026, 03, 15);
        var endAtDates = new List<DateTime> { new(2026, 03, 14), new(2026, 03, 16), new(2026, 03, 17) };
        var expectedEndAtDate = new DateTime(2026, 03, 14);
        var notExpectedEndDate = new DateTime(2026, 03, 16);
        var events = CreateEvents(count: endAtDates.Count, endAtDates: endAtDates);

        _repository
            .Setup(repository => repository.GetAllAsync(cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(events);

        // Act
        var result = await _service.GetAllAsync(title: null, from: null, to: searchEndAt, page: 1, pageSize: 10, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<ServiceResponse<PaginatedResult<Event>>>(result);
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Items);
        Assert.Contains(expectedEndAtDate, result.Data.Items.Select(@event => @event.EndAt));
        Assert.DoesNotContain(notExpectedEndDate, result.Data.Items.Select(@event => @event.EndAt));

        _repository
            .Verify(repository => repository.GetAllAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithFilterBothStartAtAndEndAt_ReturnsServiceResponseWithSuccessAndCorrectValues()
    {
        // Arrange
        //  14, (15,  16, 17) 
        //       ||
        // (24,  25), 26, 27
        // Only item with StartAt: 15 and EndAt: 25 will be in result
        var searchStartAt = new DateTime(2026, 03, 15);
        var searchEndAt = new DateTime(2026, 03, 25);
        var startAtDates = new List<DateTime> { new(2026, 03, 14), new(2026, 03, 15), new(2026, 03, 16), new(2026, 03, 17) };
        var endAtDates = new List<DateTime> { new(2026, 03, 24), new(2026, 03, 25), new(2026, 03, 26), new(2026, 03, 27) };
        var expectedStartAtDate = new DateTime(2026, 03, 15);
        var expectedEndAtDate = new DateTime(2026, 03, 25);
        var notExpectedStartAtDate = new DateTime(2026, 03, 14);
        var notExpectedEndAtDate = new DateTime(2026, 03, 26);

        var events = CreateEvents(count: startAtDates.Count, startAtDates: startAtDates, endAtDates: endAtDates);

        _repository
            .Setup(repository => repository.GetAllAsync(cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(events);

        // Act
        var result = await _service.GetAllAsync(title: null, from: searchStartAt, to: searchEndAt, page: 1, pageSize: 10, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<ServiceResponse<PaginatedResult<Event>>>(result);
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Items, @event => @event.StartAt == expectedStartAtDate && @event.EndAt == expectedEndAtDate);
        Assert.DoesNotContain(notExpectedStartAtDate, result.Data.Items.Select(@event => @event.StartAt));
        Assert.DoesNotContain(notExpectedEndAtDate, result.Data.Items.Select(@event => @event.EndAt));

        _repository
            .Verify(repository => repository.GetAllAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    public static IEnumerable<object[]> MultipleFilters()
    {
        return
        [
            [ "ing", "2026-03-26", "2026-03-27", true ],
            [ "ing", "2026-03-26", "2026-03-28", true ],
            [ "ing", "2026-03-25", "2026-03-26", true ],
            [ "ing", "2026-03-26", "2026-03-26", false ],
            [ "jogging", "2026-03-26", "2026-03-27", true ],
            [ "JOGGING", "2026-03-26", "2026-03-27", true ],
            [ "yo", "2026-03-26", "2026-03-27", true ],
            [ "running", "2026-03-27", "2026-03-28", true ],
            [ "run", "2026-03-27", "2026-03-28", true ],
            [ "ing", "2026-03-27", "2026-03-28", true ],
            [ "theatre", "2026-03-26", "2026-03-28", true ],
            [ "ing", "2026-03-28", "2026-03-29", false ],
            [ "jog", "2026-03-26", "2026-03-27", true ]
        ];
    }

    [Theory]
    [MemberData(nameof(MultipleFilters))]
    public async Task GetAllAsync_WithFilterAllTitleStartAtAndEndAt_ReturnsServiceResponseWithSuccessAndCorrectValues(string searchTitle, DateTime searchStartAt, DateTime searchEndAt, bool isInclude)
    {
        // Arrange
        var events = new List<Event>()
        {
            new() { Id = Guid.NewGuid(), Title = "Jogging", StartAt = new DateTime(2026, 03, 26), EndAt = new DateTime(2026, 03, 27) },
            new() { Id = Guid.NewGuid(), Title = "Theatre", StartAt = new DateTime(2026, 03, 26), EndAt = new DateTime(2026, 03, 28) },
            new() { Id = Guid.NewGuid(), Title = "Morning jog", StartAt = new DateTime(2026, 03, 25), EndAt = new DateTime(2026, 03, 26) },
            new() { Id = Guid.NewGuid(), Title = "JOGGING", StartAt = new DateTime(2026, 03, 26), EndAt = new DateTime(2026, 03, 27) },
            new() { Id = Guid.NewGuid(), Title = "Jogging", StartAt = new DateTime(2026, 03, 26), EndAt = new DateTime(2026, 03, 28) },
            new() { Id = Guid.NewGuid(), Title = "Yoga", StartAt = new DateTime(2026, 03, 26), EndAt = new DateTime(2026, 03, 27) },
            new() { Id = Guid.NewGuid(), Title = "Running", StartAt = new DateTime(2026, 03, 27), EndAt = new DateTime(2026, 03, 28) }
        };

        _repository
            .Setup(repository => repository.GetAllAsync(cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(events.AsQueryable());

        // Act
        var result = await _service.GetAllAsync(title: searchTitle, from: searchStartAt, to: searchEndAt, page: 1, pageSize: 10, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<ServiceResponse<PaginatedResult<Event>>>(result);
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);
        Assert.Equal(isInclude, result.Data.Items.Any(@event =>
            @event.Title.Contains(searchTitle, StringComparison.OrdinalIgnoreCase) &&
            @event.StartAt == searchStartAt &&
            @event.EndAt == searchEndAt));

        _repository
            .Verify(repository => repository.GetAllAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithPageLessThanOne_ReturnsServiceResponseWithNotSuccessAndValidationError()
    {
        // Arrange
        var expectedValidationErrorKey = "page";
        var expectedValidationErrorValue = "page can be more or equal than 1";

        // Act
        var result = await _service.GetAllAsync(null, null, null, 0, 10, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<ServiceResponse<PaginatedResult<Event>>>(result);
        Assert.False(result.Succeeded);
        Assert.True(result.ValidationErrors.ContainsKey(expectedValidationErrorKey));
        Assert.True(result.ValidationErrors.ContainsValue(expectedValidationErrorValue));

        _repository
            .Verify(repository => repository.GetAllAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_WithPageSizeLessOrEqualThanZero_ReturnsServiceResponseWithNotSuccessAndValidationError()
    {
        // Arrange
        var expectedValidationErrorKey = "pageSize";
        var expectedValidationErrorValue = "pageSize can be more or equal than 0";

        // Act
        var result = await _service.GetAllAsync(null, null, null, 1, -1, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<ServiceResponse<PaginatedResult<Event>>>(result);
        Assert.False(result.Succeeded);
        Assert.True(result.ValidationErrors.ContainsKey(expectedValidationErrorKey));
        Assert.True(result.ValidationErrors.ContainsValue(expectedValidationErrorValue));

        _repository
            .Verify(repository => repository.GetAllAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Never);
    }

    public static IEnumerable<object?[]> DifferentDates()
    {
        return
        [
            [ null,                                  null,                                  false ],
            [ null,                                  new DateTime(2026, 03, 28),            false ],
            [ new DateTime(2026, 03, 25),            null,                                  false ],
            [ new DateTime(2026, 03, 26),            new DateTime(2026, 03, 26),            false ],
            [ new DateTime(2026, 03, 26),            new DateTime(2026, 03, 27),            false ],
            [ new DateTime(2026, 03, 30, 10, 0, 0),  new DateTime(2026, 03, 30, 10, 0, 1),  false ],
            [ new DateTime(2026, 03, 28),            new DateTime(2026, 03, 27),            true  ]
        ];
    }

    [Theory]
    [MemberData(nameof(DifferentDates))]
    public async Task GetAllAsync_WithDifferentWaysForFromAndTo_ReturnsServiceResponseWithNotAnyOrAnyValidationErrors(DateTime? from, DateTime? to, bool anyValidationErrors)
    {
        // Arrange & Act
        var result = await _service.GetAllAsync(null, from, to, 1, 10, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<ServiceResponse<PaginatedResult<Event>>>(result);
        Assert.Equal(anyValidationErrors, result.ValidationErrors.Count != 0);
    }

    [Fact]
    public async Task GetAllAsync_WithFromMoreThanTo_ReturnsServiceResponseWithNotSuccessAndValidationError()
    {
        // Arrange
        var expectedValidationErrorKey = "to";
        var expectedValidationErrorValue = "to can be more or equal than from";

        // Act
        var result = await _service.GetAllAsync(null, new DateTime(2026, 01, 30), new DateTime(2026, 01, 29), 1, 10, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<ServiceResponse<PaginatedResult<Event>>>(result);
        Assert.False(result.Succeeded);
        Assert.True(result.ValidationErrors.ContainsKey(expectedValidationErrorKey));
        Assert.True(result.ValidationErrors.ContainsValue(expectedValidationErrorValue));

        _repository
            .Verify(repository => repository.GetAllAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_WithPageLessThanOneAndPageSizeLessThanZeroAndToMoreThanFrom_Returns_ServiceResponseWithNotSuccessAndThreeValidationErrors()
    {
        // Arrange & Act
        var result = await _service.GetAllAsync(null, new DateTime(2026, 01, 30), new DateTime(2026, 01, 29), -1, -1, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<ServiceResponse<PaginatedResult<Event>>>(result);
        Assert.False(result.Succeeded);
        Assert.Equal(3, result.ValidationErrors.Count);
        Assert.True(result.ValidationErrors.ContainsKey("page"));
        Assert.True(result.ValidationErrors.ContainsValue("page can be more or equal than 1"));
        Assert.True(result.ValidationErrors.ContainsKey("pageSize"));
        Assert.True(result.ValidationErrors.ContainsValue("pageSize can be more or equal than 0"));
        Assert.True(result.ValidationErrors.ContainsKey("to"));
        Assert.True(result.ValidationErrors.ContainsValue("to can be more or equal than from"));

        _repository
            .Verify(repository => repository.GetAllAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_WithRepositoryThrowsException_ReturnsServiceResponseWithNotSuccess()
    {
        // Arrange
        var expectedExceptionMessage = "Database error";

        _repository
            .Setup(repository => repository.GetAllAsync(cancellationToken: TestContext.Current.CancellationToken))
            .ThrowsAsync(new Exception(expectedExceptionMessage));

        // Act
        var result = await _service.GetAllAsync(null, null, null, 1, 10, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<ServiceResponse<PaginatedResult<Event>>>(result);
        Assert.False(result.Succeeded);
        Assert.Contains(expectedExceptionMessage, result.Errors);

        _repository
            .Verify(repository => repository.GetAllAsync(cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithCorrectId_ReturnsServiceResponseWithSuccessAndCorrectValue()
    {
        // Arrange
        var id = Guid.NewGuid();
        var @event = new Event()
        {
            Id = id,
            Title = "Jumping",
            Description = "Jumping with other beautiful women",
            StartAt = new DateTime(2026, 03, 26),
            EndAt = new DateTime(2026, 03, 27)
        };

        _repository
            .Setup(repository => repository.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(@event);

        // Act
        var result = await _service.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<ServiceResponse<Event>>(result);
        Assert.True(result.Succeeded);
        Assert.Equal(@event, result.Data);

        _repository
            .Verify(repository => repository.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithNotExistsId_ReturnsServiceResponseWithNotSuccessAndErrorMessage()
    {
        // Arrange
        var id = Guid.NewGuid();
        var expectedExceptionMessage = $"Event with Id: {id} not found";

        _repository
            .Setup(repository => repository.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken))
            .ThrowsAsync(new KeyNotFoundException(expectedExceptionMessage));

        // Act
        var result = await _service.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<ServiceResponse<Event>>(result);
        Assert.False(result.Succeeded);
        Assert.Contains(expectedExceptionMessage, result.Errors);

        _repository
            .Verify(repository => repository.GetByIdAsync(id, cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithNotExistsEvent_ReturnsServiceResponseWithNotSuccessAndErrorMessage()
    {
        // Arrange
        var expectedExceptionMessage = "Event not found";
        var eventId = Guid.NewGuid();
        _repository
            .Setup(repository => repository.GetByIdAsync(eventId, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync((Event)null!);

        // Act
        var result = await _service.GetByIdAsync(eventId, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<ServiceResponse<Event>>(result);
        Assert.False(result.Succeeded);
        Assert.Contains(expectedExceptionMessage, result.Errors);

        _repository
            .Verify(repository => repository.GetByIdAsync(eventId, cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithValidEvent_ReturnsServiceResponseWithSuccessAndEvent()
    {
        // Arrange
        var @event = new Event()
        {
            Id = Guid.NewGuid(),
            Title = "Cycling",
            Description = "Cycling with other crazy people",
            StartAt = new DateTime(2026, 05, 25),
            EndAt = new DateTime(2026, 05, 29)
        };

        _repository
            .Setup(repository => repository.CreateAsync(It.IsAny<Event>(), cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CreateAsync(@event, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<ServiceResponse<Event>>(result);
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);
        Assert.Equal(@event.Title, result.Data.Title);
        Assert.Equal(@event.Description, result.Data.Description);
        Assert.Equal(@event.StartAt, result.Data.StartAt);
        Assert.Equal(@event.EndAt, result.Data.EndAt);

        _repository
            .Verify(repository => repository.CreateAsync(It.IsAny<Event>(), cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithRepositoryReturnsFalse_ReturnsServiceResponseWithNotSuccess()
    {
        // Arrange
        var expectedErrorMessage = "Cannot create event";
        var @event = new Event()
        {
            Id = Guid.NewGuid(),
            Title = "Cycling",
            Description = "Cycling with other crazy people",
            StartAt = new DateTime(2026, 05, 25),
            EndAt = new DateTime(2026, 05, 29)
        };

        _repository
            .Setup(repository => repository.CreateAsync(It.IsAny<Event>(), cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(false);

        // Act
        var result = await _service.CreateAsync(@event, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<ServiceResponse<Event>>(result);
        Assert.False(result.Succeeded);
        Assert.Contains(expectedErrorMessage, result.Errors);

        _repository
            .Verify(repository => repository.CreateAsync(It.IsAny<Event>(), cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithRepositoryThrowsException_ReturnsServiceResponseWithNotSuccess()
    {
        // Arrange
        var expectedExceptionMessage = "Database error";
        var @event = new Event()
        {
            Id = Guid.NewGuid(),
            Title = "Cycling",
            Description = "Cycling with other crazy people",
            StartAt = new DateTime(2026, 05, 25),
            EndAt = new DateTime(2026, 05, 29)
        };

        _repository
            .Setup(repository => repository.CreateAsync(@event, cancellationToken: TestContext.Current.CancellationToken))
            .ThrowsAsync(new Exception(expectedExceptionMessage));

        // Act
        var result = await _service.CreateAsync(@event, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<ServiceResponse<Event>>(result);
        Assert.False(result.Succeeded);
        Assert.Contains(expectedExceptionMessage, result.Errors);

        _repository
            .Verify(repository => repository.CreateAsync(@event, cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithValidEvent_ReturnsServiceResponseWithSuccess()
    {
        // Arrange
        var id = Guid.NewGuid();
        var @event = new Event()
        {
            Id = id,
            Title = "Jumping",
            Description = "Jumping with other beautiful women",
            StartAt = new DateTime(2026, 03, 26),
            EndAt = new DateTime(2026, 03, 27)
        };

        _repository
            .Setup(repository => repository.UpdateAsync(It.IsAny<Event>(), cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(true);

        // Act
        var result = await _service.UpdateAsync(@event, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<ServiceResponse>(result);
        Assert.True(result.Succeeded);

        _repository
            .Verify(repository => repository.UpdateAsync(It.IsAny<Event>(), cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNotExistsId_ReturnsServiceResponseWithNotSuccessAndErrorMessage()
    {
        // Arrange
        var id = Guid.NewGuid();
        var exceptionMessage = $"Event with Id: {id} not found";
        var @event = new Event()
        {
            Id = id,
            Title = "Jogging",
            Description = "Jogging with other strong men",
            StartAt = new DateTime(2026, 06, 24),
            EndAt = new DateTime(2026, 06, 28)
        };

        _repository
            .Setup(repository => repository.UpdateAsync(It.IsAny<Event>(), cancellationToken: TestContext.Current.CancellationToken))
            .ThrowsAsync(new KeyNotFoundException(exceptionMessage));

        // Act
        var result = await _service.UpdateAsync(@event, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<ServiceResponse>(result);
        Assert.False(result.Succeeded);
        Assert.Contains(exceptionMessage, result.Errors);

        _repository
            .Verify(repository => repository.UpdateAsync(It.IsAny<Event>(), cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithRepositoryReturnsFalse_ReturnsServiceResponseWithNotSuccess()
    {
        // Arrange
        var expectedErrorMessage = "Cannot update event";
        var id = Guid.NewGuid();
        var @event = new Event()
        {
            Id = id,
            Title = "Cycling",
            Description = "Cycling with other crazy people",
            StartAt = new DateTime(2026, 05, 25),
            EndAt = new DateTime(2026, 05, 29)
        };

        _repository
            .Setup(repository => repository.UpdateAsync(@event, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(false);

        // Act
        var result = await _service.UpdateAsync(@event, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<ServiceResponse>(result);
        Assert.False(result.Succeeded);
        Assert.Contains(expectedErrorMessage, result.Errors);

        _repository
            .Verify(repository => repository.UpdateAsync(@event, cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_WithValidId_ReturnsServiceResponseWithSuccess()
    {
        // Arrange
        var id = Guid.NewGuid();

        _repository
            .Setup(repository => repository.RemoveAsync(id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(true);

        // Act
        var result = await _service.RemoveAsync(id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<ServiceResponse>(result);
        Assert.True(result.Succeeded);

        _repository
            .Verify(repository => repository.RemoveAsync(id, cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_WithRepositoryReturnsFalse_ReturnsServiceResponseWithNotSuccess()
    {
        // Arrange
        var expectedErrorMessage = "Cannot remove event";
        var id = Guid.NewGuid();

        _repository
            .Setup(repository => repository.RemoveAsync(id, cancellationToken: TestContext.Current.CancellationToken))
            .ReturnsAsync(false);

        // Act
        var result = await _service.RemoveAsync(id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<ServiceResponse>(result);
        Assert.False(result.Succeeded);
        Assert.Contains(expectedErrorMessage, result.Errors);

        _repository
            .Verify(repository => repository.RemoveAsync(id, cancellationToken: TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_WithRepositoryThrowsException_ReturnsServiceResponseWithNotSuccess()
    {
        // Arrange
        var expectedExceptionMessage = "Database error";
        var id = Guid.NewGuid();

        _repository
            .Setup(repository => repository.RemoveAsync(id, cancellationToken: TestContext.Current.CancellationToken))
            .ThrowsAsync(new Exception(expectedExceptionMessage));

        // Act
        var result = await _service.RemoveAsync(id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<ServiceResponse>(result);
        Assert.False(result.Succeeded);
        Assert.Contains(expectedExceptionMessage, result.Errors);

        _repository
            .Verify(repository => repository.RemoveAsync(id, cancellationToken: TestContext.Current.CancellationToken), Times.Once);

    }

    private static IQueryable<Event> CreateEvents(int count, List<string>? titles = null, List<DateTime>? startAtDates = null, List<DateTime>? endAtDates = null)
    {
        var list = new List<Event>();
        for (int i = 0; i < count; i++)
        {
            list.Add(new Event
            {
                Id = Guid.NewGuid(),
                Title = titles != null ? titles[i] : i.ToString(),
                StartAt = startAtDates != null ? startAtDates[i] : DateTime.MinValue,
                EndAt = endAtDates != null ? endAtDates[i] : DateTime.MaxValue
            });
        }

        return list.AsQueryable();
    }
}
