using BGA.API.Dtos;
using BGA.API.E2ETests.Infrastructure;
using BGA.API.E2ETests.Models;
using BGA.Domain.Models;
using BGA.Domain.Models.Enums;
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

    [Fact]
    public async Task GET_WithoutFiltersShouldReturnsEvents()
    {
        await _factory.ResetDatabaseAsync();

        // Arrange
        var events = new[]
        {
            new Event(
                title: "First event",
                description: "Description for first event",
                startAt: new DateTimeOffset(2026, 1, 14, 10, 0, 0, TimeSpan.Zero),
                endAt: new DateTimeOffset(2026, 1, 14, 12, 0, 0, TimeSpan.Zero),
                totalSeats: 5),
            new Event(
                title: "Second event",
                description: "Description for second event",
                startAt: new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero),
                endAt: new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero),
                totalSeats: 6),
            new Event(
                title: "Third event",
                description: null,
                startAt: new DateTimeOffset(2026, 1, 16, 10, 0, 0, TimeSpan.Zero),
                endAt: new DateTimeOffset(2026, 1, 16, 12, 0, 0, TimeSpan.Zero),
                totalSeats: 7)
        };

        await using (var arrangeContext = _factory.CreateContext())
        {
            await arrangeContext.Events.AddRangeAsync(events, TestContext.Current.CancellationToken);
            await arrangeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act
        var httpResponse = await _client.GetAsync("/events", TestContext.Current.CancellationToken);
        var responseBody = await httpResponse.Content.ReadFromJsonAsync<PaginatedResult<EventDto>>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        Assert.NotNull(responseBody);

        await using var verifyContext = _factory.CreateContext();
        var savedEvents = await verifyContext.Events
            .AsNoTracking()
            .OrderBy(e => e.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        var responseItems = responseBody.Items
            .OrderBy(e => e.Id)
            .ToList();

        Assert.Equal(savedEvents.Count, responseBody.TotalItems);
        Assert.Equal(1, responseBody.PageNumber);
        Assert.Equal(savedEvents.Count, responseBody.PageSize);
        Assert.Equal(savedEvents.Count, responseItems.Count);

        for (var i = 0; i < savedEvents.Count; i++)
        {
            Assert.Equal(savedEvents[i].Id, responseItems[i].Id);
            Assert.Equal(savedEvents[i].Title, responseItems[i].Title);
            Assert.Equal(savedEvents[i].Description, responseItems[i].Description);
            Assert.Equal(savedEvents[i].StartAt, responseItems[i].StartAt);
            Assert.Equal(savedEvents[i].EndAt, responseItems[i].EndAt);
            Assert.Equal(savedEvents[i].TotalSeats, responseItems[i].TotalSeats);
            Assert.Equal(savedEvents[i].AvailableSeats, responseItems[i].AvailableSeats);
        }
    }

    [Fact]
    public async Task POST_Book_ShouldCreateBooking()
    {
        await _factory.ResetDatabaseAsync();

        // Arrange
        var existingEvent = new Event(
            title: "Bookable event",
            description: "Description for bookable event",
            startAt: new DateTimeOffset(2026, 1, 17, 10, 0, 0, TimeSpan.Zero),
            endAt: new DateTimeOffset(2026, 1, 17, 12, 0, 0, TimeSpan.Zero),
            totalSeats: 3);

        await using (var arrangeContext = _factory.CreateContext())
        {
            await arrangeContext.Events.AddAsync(existingEvent, TestContext.Current.CancellationToken);
            await arrangeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act
        var httpResponse = await _client.PostAsync($"/events/{existingEvent.Id}/book", null, TestContext.Current.CancellationToken);
        var responseBody = await httpResponse.Content.ReadFromJsonAsync<BookingResponse>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, httpResponse.StatusCode);
        Assert.NotNull(responseBody);
        Assert.NotEqual(Guid.Empty, responseBody.Id);
        Assert.Equal(existingEvent.Id, responseBody.EventId);
        Assert.Equal("pending", responseBody.Status);
        Assert.NotNull(httpResponse.Headers.Location);
        Assert.EndsWith($"/Bookings/{responseBody.Id}", httpResponse.Headers.Location.ToString());

        await using var verifyContext = _factory.CreateContext();
        var savedBooking = await verifyContext.Bookings
            .AsNoTracking()
            .SingleAsync(b => b.Id == responseBody.Id, TestContext.Current.CancellationToken);
        var savedEvent = await verifyContext.Events
            .AsNoTracking()
            .SingleAsync(e => e.Id == existingEvent.Id, TestContext.Current.CancellationToken);

        Assert.Equal(responseBody.EventId, savedBooking.EventId);
        Assert.Equal(BookingStatus.Pending, savedBooking.Status);
        Assert.Equal(3, savedEvent.TotalSeats);
        Assert.Equal(2, savedEvent.AvailableSeats);
        Assert.True(savedBooking.CreatedAt > DateTimeOffset.MinValue);
        Assert.Null(savedBooking.ProcessedAt);
    }
}
