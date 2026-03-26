using BGA.API.Application;
using BGA.API.Application.Extensions;
using BGA.API.Application.Services.Implementations;
using BGA.API.Infrastructure.Models;
using BGA.API.Infrastructure.Repositories.Interfaces;
using BGA.API.Presentation.Dtos;
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
    public void Create_WithValidAddEventDto_ReturnsServiceResponseWithEventDto()
    {
        // Arrange
        var dto = new AddEventDto()
        {
            Title = "Cycling",
            Description = "Cycling with other crazy people",
            StartAt = new DateTime(2026, 05, 25),
            EndAt = new DateTime(2026, 05, 29)
        };

        _repository
            .Setup(repository => repository.Create(It.IsAny<Event>()))
            .Returns(true);

        // Act
        var result = _service.Create(dto);

        // Assert
        Assert.IsType<ServiceResponse<EventDto>>(result);
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);
        Assert.Equal(dto.Title, result.Data.Title);
        Assert.Equal(dto.Description, result.Data.Description);
        Assert.Equal(dto.StartAt, result.Data.StartAt);
        Assert.Equal(dto.EndAt, result.Data.EndAt);

        _repository
            .Verify(repository => repository.Create(It.IsAny<Event>()), Times.Once);
    }

    [Fact]
    public void Update_WithValidPutEventDto_ReturnsServiceResponseWithSuccess()
    {
        // Arrange
        var id = 1;
        var dto = new PutEventDto()
        {
            Title = "Jumping",
            Description = "Jumping with other beautiful women",
            StartAt = new DateTime(2026, 03, 26),
            EndAt = new DateTime(2026, 03, 27)
        };

        _repository
            .Setup(repository => repository.Update(id, It.IsAny<Event>()))
            .Returns(true);

        // Act
        var result = _service.Update(id, dto);

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
        var dto = new PutEventDto()
        {
            Title = "Jogging",
            Description = "Jogging with other strong men",
            StartAt = new DateTime(2026, 06, 24),
            EndAt = new DateTime(2026, 06, 28)
        };

        _repository
            .Setup(repository => repository.Update(id, It.IsAny<Event>()))
            .Throws(new KeyNotFoundException(exceptionMessage));

        // Act
        var result = _service.Update(id, dto);

        // Assert
        Assert.IsType<ServiceResponse>(result);
        Assert.False(result.Succeeded);
        Assert.Contains(exceptionMessage, result.Errors);

        _repository
            .Verify(repository => repository.Update(id, It.IsAny<Event>()), Times.Once);
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
    public void GetById_WithNotExistsId_ReturnsServiceResponseWithNotSuccessAndErrorMessage()
    {
        // Arrange
        var id = 1;
        var exceptionMessage = $"Event with Id: {id} not found";

        _repository
            .Setup(repository => repository.GetById(id))
            .Throws(new KeyNotFoundException(exceptionMessage));

        // Act
        var result = _service.GetById(id);

        // Assert
        Assert.IsType<ServiceResponse<EventDto>>(result);
        Assert.False(result.Succeeded);
        Assert.Contains(exceptionMessage, result.Errors);

        _repository
            .Verify(repository => repository.GetById(id), Times.Once);
    }
}
