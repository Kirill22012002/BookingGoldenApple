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
    public void GetAll_WithPageAndPageSize_ReturnsServiceResponseWithSuccessAndCorrectCounts()
    {
        // Arrange
        var page = 1;
        var pageSize = 10;
        var totalItems = 12;
        var events = CreateEvents(count: totalItems);

        _repository
            .Setup(repository => repository.GetAll())
            .Returns(events);

        // Act
        var result = _service.GetAll(title: null, from: null, to: null, page: page, pageSize: pageSize);

        // Assert
        Assert.IsType<ServiceResponse<PaginatedResult<Event>>>(result);
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);
        Assert.Equal(totalItems, result.Data.TotalItems);
        Assert.Equal(page, result.Data.PageNumber);
        Assert.Equal(pageSize, result.Data.PageSize);
        Assert.Equal(pageSize, result.Data.Items.Count());

        _repository
            .Verify(repository => repository.GetAll(), Times.Once);
    }

    [Fact]
    public void GetAll_WithFilterByTitle_ReturnsServiceResponseWithSuccessAndCorrectValues()
    {
        // Arrange
        var searchSubstring = "ing";
        var titles = new List<string> { "Jogging", "Running", "Theathre", "JUMPING", "Basketball" };
        var expectedTitles = new List<string> { "Jogging", "Running", "JUMPING" };
        var notExpectedTitle = "Theathre";
        var events = CreateEvents(count: titles.Count, titles: titles);

        _repository
            .Setup(repository => repository.GetAll())
            .Returns(events);

        // Act
        var result = _service.GetAll(title: searchSubstring, from: null, to: null, page: 1, pageSize: 10);

        // Assert
        Assert.IsType<ServiceResponse<PaginatedResult<Event>>>(result);
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);
        Assert.Equal(expectedTitles.Count, result.Data.Items.Count());
        Assert.All(result.Data.Items, @event => expectedTitles.Contains(@event.Title));
        Assert.DoesNotContain(notExpectedTitle, result.Data.Items.Select(@event => @event.Title));

        _repository
            .Verify(repository => repository.GetAll(), Times.Once);
    }

    [Fact]
    public void GetAll_WithFilterByStartAt_ReturnsServiceResponseWithSuccessAndCorrectValues()
    {
        // Arrange
        var searchStartAt = new DateTime(2026, 03, 15);
        var startAtDates = new List<DateTime> { new(2026, 03, 14), new(2026, 03, 15), new(2026, 03, 16) };
        var expectedStartAtDates = new List<DateTime> { new(2026, 03, 15), new(2026, 03, 16) };
        var notExpectedStartAtDate = new DateTime(2026, 03, 14);
        var events = CreateEvents(count: startAtDates.Count, startAtDates: startAtDates);

        _repository
            .Setup(repository => repository.GetAll())
            .Returns(events);

        // Act
        var result = _service.GetAll(title: null, from: searchStartAt, to: null, page: 1, pageSize: 10);

        // Assert
        Assert.IsType<ServiceResponse<PaginatedResult<Event>>>(result);
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);
        Assert.Equal(expectedStartAtDates.Count, result.Data.Items.Count());
        Assert.All(result.Data.Items, @event => expectedStartAtDates.Contains(@event.StartAt));
        Assert.DoesNotContain(notExpectedStartAtDate, result.Data.Items.Select(@event => @event.StartAt));

        _repository
            .Verify(repository => repository.GetAll(), Times.Once);
    }

    [Fact]
    public void GetAll_WithFilterByEndAt_ReturnsServiceResponseWithSuccessAndCorrectValues()
    {
        // Arrange
        var searchEndAt = new DateTime(2026, 03, 15);
        var endAtDates = new List<DateTime> { new(2026, 03, 14), new(2026, 03, 16), new(2026, 03, 17) };
        var expectedEndAtDate = new DateTime(2026, 03, 14);
        var notExpectedEndDate = new DateTime(2026, 03, 16);
        var events = CreateEvents(count: endAtDates.Count, endAtDates: endAtDates);

        _repository
            .Setup(repository => repository.GetAll())
            .Returns(events);

        // Act
        var result = _service.GetAll(title: null, from: null, to: searchEndAt, page: 1, pageSize: 10);

        // Assert
        Assert.IsType<ServiceResponse<PaginatedResult<Event>>>(result);
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Items);
        Assert.Contains(expectedEndAtDate, result.Data.Items.Select(@event => @event.EndAt));
        Assert.DoesNotContain(notExpectedEndDate, result.Data.Items.Select(@event => @event.EndAt));

        _repository
            .Verify(repository => repository.GetAll(), Times.Once);
    }

    [Fact]
    public void GetAll_WithFilterBothStartAtAndEndAt_ReturnsServiceResponseWithSuccessAndCorrectValues()
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
            .Setup(repository => repository.GetAll())
            .Returns(events);

        // Act
        var result = _service.GetAll(title: null, from: searchStartAt, to: searchEndAt, page: 1, pageSize: 10);

        // Assert
        Assert.IsType<ServiceResponse<PaginatedResult<Event>>>(result);
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Items, @event => @event.StartAt == expectedStartAtDate && @event.EndAt == expectedEndAtDate);
        Assert.DoesNotContain(notExpectedStartAtDate, result.Data.Items.Select(@event => @event.StartAt));
        Assert.DoesNotContain(notExpectedEndAtDate, result.Data.Items.Select(@event => @event.EndAt));

        _repository
            .Verify(repository => repository.GetAll(), Times.Once);
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
    public void GetAll_WithFilterAllTitleStartAtAndEndAt_ReturnsServiceResponseWithSuccessAndCorrectValues(string searchTitle, DateTime searchStartAt, DateTime searchEndAt, bool isInclude)
    {
        // Arrange
        var events = new List<Event>()
        {
            new() { Id = 1, Title = "Jogging", StartAt = new DateTime(2026, 03, 26), EndAt = new DateTime(2026, 03, 27) },
            new() { Id = 2, Title = "Theatre", StartAt = new DateTime(2026, 03, 26), EndAt = new DateTime(2026, 03, 28) },
            new() { Id = 3, Title = "Morning jog", StartAt = new DateTime(2026, 03, 25), EndAt = new DateTime(2026, 03, 26) },
            new() { Id = 4, Title = "JOGGING", StartAt = new DateTime(2026, 03, 26), EndAt = new DateTime(2026, 03, 27) },
            new() { Id = 5, Title = "Jogging", StartAt = new DateTime(2026, 03, 26), EndAt = new DateTime(2026, 03, 28) },
            new() { Id = 6, Title = "Yoga", StartAt = new DateTime(2026, 03, 26), EndAt = new DateTime(2026, 03, 27) },
            new() { Id = 7, Title = "Running", StartAt = new DateTime(2026, 03, 27), EndAt = new DateTime(2026, 03, 28) }
        };

        _repository
            .Setup(repository => repository.GetAll())
            .Returns(events.AsQueryable());

        // Act
        var result = _service.GetAll(title: searchTitle, from: searchStartAt, to: searchEndAt, page: 1, pageSize: 10);

        // Assert
        Assert.IsType<ServiceResponse<PaginatedResult<Event>>>(result);
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);
        Assert.Equal(isInclude, result.Data.Items.Any(@event =>
            @event.Title.Contains(searchTitle, StringComparison.OrdinalIgnoreCase) &&
            @event.StartAt == searchStartAt &&
            @event.EndAt == searchEndAt));

        _repository
            .Verify(repository => repository.GetAll(), Times.Once);
    }

    [Fact]
    public void GetAll_WithPageLessThanOne_ReturnsServiceResponseWithNotSuccessAndValidationError()
    {
        // Arrange
        var expectedValidationErrorKey = "page";
        var expectedValidationErrorValue = "page can be more or equal than 1";

        // Act
        var result = _service.GetAll(null, null, null, 0, 10);

        // Assert
        Assert.IsType<ServiceResponse<PaginatedResult<Event>>>(result);
        Assert.False(result.Succeeded);
        Assert.True(result.ValidationErrors.ContainsKey(expectedValidationErrorKey));
        Assert.True(result.ValidationErrors.ContainsValue(expectedValidationErrorValue));

        _repository
            .Verify(repository => repository.GetAll(), Times.Never);
    }

    [Fact]
    public void GetAll_WithPageSizeLessOrEqualThanZero_ReturnsServiceResponseWithNotSuccessAndValidationError()
    {
        // Arrange
        var expectedValidationErrorKey = "pageSize";
        var expectedValidationErrorValue = "pageSize can be more or equal than 0";

        // Act
        var result = _service.GetAll(null, null, null, 1, -1);

        // Assert
        Assert.IsType<ServiceResponse<PaginatedResult<Event>>>(result);
        Assert.False(result.Succeeded);
        Assert.True(result.ValidationErrors.ContainsKey(expectedValidationErrorKey));
        Assert.True(result.ValidationErrors.ContainsValue(expectedValidationErrorValue));

        _repository
            .Verify(repository => repository.GetAll(), Times.Never);
    }

    public static IEnumerable<object?[]> DifferentDates()
    {
        return
        [
            [ null,                         null,                         false ],
            [ null,                         new DateTime(2026, 03, 28),   false ],
            [ new DateTime(2026, 03, 25),   null,                         false ],
            [ new DateTime(2026, 03, 26),   new DateTime(2026, 03, 26),   false ],
            [ new DateTime(2026, 03, 26),   new DateTime(2026, 03, 27),   false ],
            [ new DateTime(2026, 03, 28),   new DateTime(2026, 03, 27),   true  ]
        ];
    }

    [Theory]
    [MemberData(nameof(DifferentDates))]
    public void GetAll_WithDifferentWaysForFromAndTo_ReturnsServiceResponseWithNotAnyOrAnyValidationErrors(DateTime? from, DateTime? to, bool anyValidationErrors)
    {
        // Arrange & Act
        var result = _service.GetAll(null, from, to, 1, 10);

        // Assert
        Assert.IsType<ServiceResponse<PaginatedResult<Event>>>(result);
        Assert.Equal(anyValidationErrors, result.ValidationErrors.Count != 0);
    }

    [Fact]
    public void GetAll_WithFromMoreThanTo_ReturnsServiceResponseWithNotSuccessAndValidationError()
    {
        // Arrange
        var expectedValidationErrorKey = "to";
        var expectedValidationErrorValue = "to can be more or equal than from";

        // Act
        var result = _service.GetAll(null, new DateTime(2026, 01, 30), new DateTime(2026, 01, 29), 1, 10);

        // Assert
        Assert.IsType<ServiceResponse<PaginatedResult<Event>>>(result);
        Assert.False(result.Succeeded);
        Assert.True(result.ValidationErrors.ContainsKey(expectedValidationErrorKey));
        Assert.True(result.ValidationErrors.ContainsValue(expectedValidationErrorValue));

        _repository
            .Verify(repository => repository.GetAll(), Times.Never);
    }

    [Fact]
    public void GetAll_WithPageLessThanOneAndPageSizeLessThanZeroAndToMoreThanFrom_Returns_ServiceResponseWithNotSuccessAndThreeValidationErrors()
    {
        // Arrange & Act
        var result = _service.GetAll(null, new DateTime(2026, 01, 30), new DateTime(2026, 01, 29), -1, -1);

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
            .Verify(repository => repository.GetAll(), Times.Never);
    }

    [Fact]
    public void GetAll_WithRepositoryThrowsException_ReturnsServiceResponseWithNotSuccess()
    {
        // Arrange
        var expectedExceptionMessage = "Database error";

        _repository
            .Setup(repository => repository.GetAll())
            .Throws(new Exception(expectedExceptionMessage));

        // Act
        var result = _service.GetAll(null, null, null, 1, 10);

        // Assert
        Assert.IsType<ServiceResponse<PaginatedResult<Event>>>(result);
        Assert.False(result.Succeeded);
        Assert.Contains(expectedExceptionMessage, result.Errors);

        _repository
            .Verify(repository => repository.GetAll(), Times.Once);
    }

    [Fact]
    public void GetById_WithCorrectId_ReturnsServiceResponseWithSuccessAndCorrectValue()
    {
        // Arrange
        var id = 1;
        var @event = new Event()
        {
            Id = id,
            Title = "Jumping",
            Description = "Jumping with other beautiful women",
            StartAt = new DateTime(2026, 03, 26),
            EndAt = new DateTime(2026, 03, 27)
        };

        _repository
            .Setup(repository => repository.GetById(id))
            .Returns(@event);

        // Act
        var result = _service.GetById(id);

        // Assert
        Assert.IsType<ServiceResponse<Event>>(result);
        Assert.True(result.Succeeded);
        Assert.Equal(@event, result.Data);

        _repository
            .Verify(repository => repository.GetById(id), Times.Once);
    }

    [Fact]
    public void GetById_WithNotExistsId_ReturnsServiceResponseWithNotSuccessAndErrorMessage()
    {
        // Arrange
        var id = 1;
        var expectedExceptionMessage = $"Event with Id: {id} not found";

        _repository
            .Setup(repository => repository.GetById(id))
            .Throws(new KeyNotFoundException(expectedExceptionMessage));

        // Act
        var result = _service.GetById(id);

        // Assert
        Assert.IsType<ServiceResponse<Event>>(result);
        Assert.False(result.Succeeded);
        Assert.Contains(expectedExceptionMessage, result.Errors);

        _repository
            .Verify(repository => repository.GetById(id), Times.Once);
    }

    [Fact]
    public void Create_WithValidEvent_ReturnsServiceResponseWithSuccessAndEvent()
    {
        // Arrange
        var @event = new Event()
        {
            Id = 1,
            Title = "Cycling",
            Description = "Cycling with other crazy people",
            StartAt = new DateTime(2026, 05, 25),
            EndAt = new DateTime(2026, 05, 29)
        };

        _repository
            .Setup(repository => repository.Create(It.IsAny<Event>()))
            .Returns(true);

        // Act
        var result = _service.Create(@event);

        // Assert
        Assert.IsType<ServiceResponse<Event>>(result);
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);
        Assert.Equal(@event.Title, result.Data.Title);
        Assert.Equal(@event.Description, result.Data.Description);
        Assert.Equal(@event.StartAt, result.Data.StartAt);
        Assert.Equal(@event.EndAt, result.Data.EndAt);

        _repository
            .Verify(repository => repository.Create(It.IsAny<Event>()), Times.Once);
    }

    [Fact]
    public void Create_WithRepositoryReturnsFalse_ReturnsServiceResponseWithNotSuccess()
    {
        // Arrange
        var expectedErrorMessage = "Cannot create event";
        var @event = new Event()
        {
            Id = 1,
            Title = "Cycling",
            Description = "Cycling with other crazy people",
            StartAt = new DateTime(2026, 05, 25),
            EndAt = new DateTime(2026, 05, 29)
        };

        _repository
            .Setup(repository => repository.Create(It.IsAny<Event>()))
            .Returns(false);

        // Act
        var result = _service.Create(@event);

        // Assert
        Assert.IsType<ServiceResponse<Event>>(result);
        Assert.False(result.Succeeded);
        Assert.Contains(expectedErrorMessage, result.Errors);

        _repository
            .Verify(repository => repository.Create(It.IsAny<Event>()), Times.Once);
    }

    [Fact]
    public void Create_WithRepositoryThrowsException_ReturnsServiceResponseWithNotSuccess()
    {
        // Arrange
        var expectedExceptionMessage = "Database error";
        var @event = new Event()
        {
            Id = 1,
            Title = "Cycling",
            Description = "Cycling with other crazy people",
            StartAt = new DateTime(2026, 05, 25),
            EndAt = new DateTime(2026, 05, 29)
        };

        _repository
            .Setup(repository => repository.Create(@event))
            .Throws(new Exception(expectedExceptionMessage));

        // Act
        var result = _service.Create(@event);

        // Assert
        Assert.IsType<ServiceResponse<Event>>(result);
        Assert.False(result.Succeeded);
        Assert.Contains(expectedExceptionMessage, result.Errors);

        _repository
            .Verify(repository => repository.Create(@event), Times.Once);
    }

    [Fact]
    public void Update_WithValidEvent_ReturnsServiceResponseWithSuccess()
    {
        // Arrange
        var id = 1;
        var @event = new Event()
        {
            Id = id,
            Title = "Jumping",
            Description = "Jumping with other beautiful women",
            StartAt = new DateTime(2026, 03, 26),
            EndAt = new DateTime(2026, 03, 27)
        };

        _repository
            .Setup(repository => repository.Update(id, It.IsAny<Event>()))
            .Returns(true);

        // Act
        var result = _service.Update(id, @event);

        // Assert
        Assert.IsType<ServiceResponse>(result);
        Assert.True(result.Succeeded);

        _repository
            .Verify(repository => repository.Update(id, It.IsAny<Event>()), Times.Once);
    }

    [Fact]
    public void Update_WithNotExistsId_ReturnsServiceResponseWithNotSuccessAndErrorMessage()
    {
        // Arrange
        var id = 1;
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
            .Setup(repository => repository.Update(id, It.IsAny<Event>()))
            .Throws(new KeyNotFoundException(exceptionMessage));

        // Act
        var result = _service.Update(id, @event);

        // Assert
        Assert.IsType<ServiceResponse>(result);
        Assert.False(result.Succeeded);
        Assert.Contains(exceptionMessage, result.Errors);

        _repository
            .Verify(repository => repository.Update(id, It.IsAny<Event>()), Times.Once);
    }

    [Fact]
    public void Update_WithRepositoryReturnsFalse_ReturnsServiceResponseWithNotSuccess()
    {
        // Arrange
        var expectedErrorMessage = "Cannot update event";
        var id = 1;
        var @event = new Event()
        {
            Id = id,
            Title = "Cycling",
            Description = "Cycling with other crazy people",
            StartAt = new DateTime(2026, 05, 25),
            EndAt = new DateTime(2026, 05, 29)
        };

        _repository
            .Setup(repository => repository.Update(id, @event))
            .Returns(false);

        // Act
        var result = _service.Update(id, @event);

        // Assert
        Assert.IsType<ServiceResponse>(result);
        Assert.False(result.Succeeded);
        Assert.Contains(expectedErrorMessage, result.Errors);

        _repository
            .Verify(repository => repository.Update(id, @event), Times.Once);
    }

    [Fact]
    public void Remove_WithValidId_ReturnsServiceResponseWithSuccess()
    {
        // Arrange
        var id = 1;

        _repository
            .Setup(repository => repository.Remove(id))
            .Returns(true);

        // Act
        var result = _service.Remove(id);

        // Assert
        Assert.IsType<ServiceResponse>(result);
        Assert.True(result.Succeeded);

        _repository
            .Verify(repository => repository.Remove(id), Times.Once);
    }

    [Fact]
    public void Remove_WithRepositoryReturnsFalse_ReturnsServiceResponseWithNotSuccess()
    {
        // Arrange
        var expectedErrorMessage = "Cannot remove event";
        var id = 1;

        _repository
            .Setup(repository => repository.Remove(id))
            .Returns(false);

        // Act
        var result = _service.Remove(id);

        // Assert
        Assert.IsType<ServiceResponse>(result);
        Assert.False(result.Succeeded);
        Assert.Contains(expectedErrorMessage, result.Errors);

        _repository
            .Verify(repository => repository.Remove(id), Times.Once);
    }

    [Fact]
    public void Remove_WithRepositoryThrowsException_ReturnsServiceResponseWithNotSuccess()
    {
        // Arrange
        var expectedExceptionMessage = "Database error";
        var id = 1;

        _repository
            .Setup(repository => repository.Remove(id))
            .Throws(new Exception(expectedExceptionMessage));

        // Act
        var result = _service.Remove(id);

        // Assert
        Assert.IsType<ServiceResponse>(result);
        Assert.False(result.Succeeded);
        Assert.Contains(expectedExceptionMessage, result.Errors);

        _repository
            .Verify(repository => repository.Remove(id), Times.Once);

    }

    private static IQueryable<Event> CreateEvents(int count, List<string>? titles = null, List<DateTime>? startAtDates = null, List<DateTime>? endAtDates = null)
    {
        var list = new List<Event>();
        for (int i = 0; i < count; i++)
        {
            list.Add(new Event
            {
                Id = i + 1,
                Title = titles != null ? titles[i] : i.ToString(),
                StartAt = startAtDates != null ? startAtDates[i] : DateTime.MinValue,
                EndAt = endAtDates != null ? endAtDates[i] : DateTime.MaxValue
            });
        }

        return list.AsQueryable();
    }
}
