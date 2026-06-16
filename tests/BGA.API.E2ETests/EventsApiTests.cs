using BGA.API.Dtos;
using BGA.API.E2ETests.Infrastructure;
using BGA.API.E2ETests.Models;
using BGA.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;

namespace BGA.API.E2ETests;

public class EventsApiTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.HttpClient;
    private readonly CustomWebApplicationFactory _factory = factory;

    [Fact]
    public async Task POST_ShouldCreateEvent()
    {
        await _factory.ResetDatabaseAsync();

        // Arrange
        var request = new AddEventDto
        {
            Title = "Cycling",
            Description = "Cycling with my best friends",
            StartAt = DateTimeOffset.UtcNow.AddDays(-1),
            EndAt = DateTimeOffset.UtcNow.AddDays(1),
            TotalSeats = 4
        };

        // Act
        var httpResponse = await _client.PostAsJsonAsync("/events", request, cancellationToken: TestContext.Current.CancellationToken);
        var responseBody = await httpResponse.Content.ReadFromJsonAsync<EventResponse>(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(responseBody);

        await using var verifyContext = _factory.CreateContext();
        var saved = await verifyContext.Events
            .AsNoTracking()
            .SingleAsync(e => e.Id == responseBody.Id, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, httpResponse.StatusCode);
        Assert.Equal("Cycling", saved.Title);
        Assert.Equal("Cycling with my best friends", saved.Description);
        Assert.Equal(4, saved.TotalSeats);
        Assert.Equal(4, saved.AvailableSeats);
    }

    [Fact]
    public async Task PUT_ShouldUpdateEvent()
    {
        await _factory.ResetDatabaseAsync();

        // Arrange
        var existingEvent = new Event(
            title: "Original event",
            description: "Original description",
            startAt: new DateTimeOffset(2026, 1, 10, 10, 0, 0, TimeSpan.Zero),
            endAt: new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero),
            totalSeats: 10);

        await using (var arrangeContext = _factory.CreateContext())
        {
            await arrangeContext.Events.AddAsync(existingEvent, TestContext.Current.CancellationToken);
            await arrangeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var request = new PutEventDto
        {
            Title = "Updated event",
            Description = "Updated description",
            StartAt = new DateTimeOffset(2026, 1, 11, 14, 0, 0, TimeSpan.Zero),
            EndAt = new DateTimeOffset(2026, 1, 11, 16, 0, 0, TimeSpan.Zero)
        };

        // Act
        var httpResponse = await _client.PutAsJsonAsync($"/events/{existingEvent.Id}", request, TestContext.Current.CancellationToken);
        var responseBody = await httpResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        await using var verifyContext = _factory.CreateContext();
        var saved = await verifyContext.Events
            .AsNoTracking()
            .SingleAsync(e => e.Id == existingEvent.Id, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, httpResponse.StatusCode);
        Assert.Empty(responseBody);
        Assert.Equal("Updated event", saved.Title);
        Assert.Equal("Updated description", saved.Description);
        Assert.Equal(request.StartAt, saved.StartAt);
        Assert.Equal(request.EndAt, saved.EndAt);
        Assert.Equal(10, saved.TotalSeats);
        Assert.Equal(10, saved.AvailableSeats);
    }

    [Fact]
    public async Task DELETE_ShouldDeleteEvent()
    {
        await _factory.ResetDatabaseAsync();

        // Arrange
        var existingEvent = new Event(
            title: "Event to delete",
            description: "Description for event to delete",
            startAt: new DateTimeOffset(2026, 1, 12, 10, 0, 0, TimeSpan.Zero),
            endAt: new DateTimeOffset(2026, 1, 12, 12, 0, 0, TimeSpan.Zero),
            totalSeats: 8);

        await using (var arrangeContext = _factory.CreateContext())
        {
            await arrangeContext.Events.AddAsync(existingEvent, TestContext.Current.CancellationToken);
            await arrangeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act
        var httpResponse = await _client.DeleteAsync($"/events/{existingEvent.Id}", TestContext.Current.CancellationToken);
        var responseBody = await httpResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        await using var verifyContext = _factory.CreateContext();
        var exists = await verifyContext.Events
            .AsNoTracking()
            .AnyAsync(e => e.Id == existingEvent.Id, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, httpResponse.StatusCode);
        Assert.Empty(responseBody);
        Assert.False(exists);
    }

    [Fact]
    public async Task GET_ShouldReturnEventById()
    {
        await _factory.ResetDatabaseAsync();

        // Arrange
        var existingEvent = new Event(
            title: "Event by id",
            description: "Description for event by id",
            startAt: new DateTimeOffset(2026, 1, 13, 10, 0, 0, TimeSpan.Zero),
            endAt: new DateTimeOffset(2026, 1, 13, 12, 0, 0, TimeSpan.Zero),
            totalSeats: 6);

        await using (var arrangeContext = _factory.CreateContext())
        {
            await arrangeContext.Events.AddAsync(existingEvent, TestContext.Current.CancellationToken);
            await arrangeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act
        var httpResponse = await _client.GetAsync($"/events/{existingEvent.Id}", TestContext.Current.CancellationToken);
        var responseBody = await httpResponse.Content.ReadFromJsonAsync<EventDto>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        Assert.NotNull(responseBody);

        await using var verifyContext = _factory.CreateContext();
        var saved = await verifyContext.Events
            .AsNoTracking()
            .SingleAsync(e => e.Id == existingEvent.Id, TestContext.Current.CancellationToken);

        Assert.Equal(saved.Id, responseBody.Id);
        Assert.Equal(saved.Title, responseBody.Title);
        Assert.Equal(saved.Description, responseBody.Description);
        Assert.Equal(saved.StartAt, responseBody.StartAt);
        Assert.Equal(saved.EndAt, responseBody.EndAt);
        Assert.Equal(saved.TotalSeats, responseBody.TotalSeats);
        Assert.Equal(saved.AvailableSeats, responseBody.AvailableSeats);
    }
}
