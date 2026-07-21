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

        var admin = await AuthTestHelper.RegisterAndLoginAsync(_client, "Admin", TestContext.Current.CancellationToken);
        var request = new AddEventDto
        {
            Title = "Cycling",
            Description = "Cycling with my best friends",
            StartAt = DateTimeOffset.UtcNow.AddDays(-1),
            EndAt = DateTimeOffset.UtcNow.AddDays(1),
            TotalSeats = 4
        };

        using var httpRequest = AuthTestHelper.CreateAuthorizedRequest(HttpMethod.Post, "/events", admin.Token, request);
        var httpResponse = await _client.SendAsync(httpRequest, TestContext.Current.CancellationToken);
        var responseBody = await httpResponse.Content.ReadFromJsonAsync<EventResponse>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(responseBody);
        await using var verifyContext = _factory.CreateContext();
        var saved = await verifyContext.Events.AsNoTracking().SingleAsync(e => e.Id == responseBody.Id, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, httpResponse.StatusCode);
        Assert.Equal("Cycling", saved.Title);
        Assert.Equal(4, saved.TotalSeats);
    }

    [Fact]
    public async Task POST_WithoutToken_ShouldReturnUnauthorized()
    {
        await _factory.ResetDatabaseAsync();

        var request = new AddEventDto
        {
            Title = "Protected event",
            Description = "Should require admin token",
            StartAt = DateTimeOffset.UtcNow.AddDays(10),
            EndAt = DateTimeOffset.UtcNow.AddDays(10).AddHours(2),
            TotalSeats = 10
        };

        var response = await _client.PostAsJsonAsync("/events", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task POST_WithUserToken_ShouldReturnForbidden()
    {
        await _factory.ResetDatabaseAsync();

        var user = await AuthTestHelper.RegisterAndLoginAsync(_client, cancellationToken: TestContext.Current.CancellationToken);
        var request = new AddEventDto
        {
            Title = "Admin only event",
            Description = "Should reject regular users",
            StartAt = DateTimeOffset.UtcNow.AddDays(10),
            EndAt = DateTimeOffset.UtcNow.AddDays(10).AddHours(2),
            TotalSeats = 10
        };

        using var httpRequest = AuthTestHelper.CreateAuthorizedRequest(HttpMethod.Post, "/events", user.Token, request);
        var response = await _client.SendAsync(httpRequest, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PUT_ShouldUpdateEvent()
    {
        await _factory.ResetDatabaseAsync();

        var admin = await AuthTestHelper.RegisterAndLoginAsync(_client, "Admin", TestContext.Current.CancellationToken);
        var existingEvent = new Event("Original event", "Original description", new DateTimeOffset(2026, 10, 10, 10, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 10, 10, 12, 0, 0, TimeSpan.Zero), 10);
        await using (var arrangeContext = _factory.CreateContext())
        {
            await arrangeContext.Events.AddAsync(existingEvent, TestContext.Current.CancellationToken);
            await arrangeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var request = new PutEventDto
        {
            Title = "Updated event",
            Description = "Updated description",
            StartAt = new DateTimeOffset(2026, 10, 11, 14, 0, 0, TimeSpan.Zero),
            EndAt = new DateTimeOffset(2026, 10, 11, 16, 0, 0, TimeSpan.Zero)
        };

        using var httpRequest = AuthTestHelper.CreateAuthorizedRequest(HttpMethod.Put, $"/events/{existingEvent.Id}", admin.Token, request);
        var httpResponse = await _client.SendAsync(httpRequest, TestContext.Current.CancellationToken);

        await using var verifyContext = _factory.CreateContext();
        var saved = await verifyContext.Events.AsNoTracking().SingleAsync(e => e.Id == existingEvent.Id, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, httpResponse.StatusCode);
        Assert.Equal("Updated event", saved.Title);
        Assert.Equal(request.StartAt, saved.StartAt);
    }

    [Fact]
    public async Task DELETE_ShouldDeleteEvent()
    {
        await _factory.ResetDatabaseAsync();

        var admin = await AuthTestHelper.RegisterAndLoginAsync(_client, "Admin", TestContext.Current.CancellationToken);
        var existingEvent = new Event("Event to delete", "Description for event to delete", new DateTimeOffset(2026, 10, 12, 10, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 10, 12, 12, 0, 0, TimeSpan.Zero), 8);
        await using (var arrangeContext = _factory.CreateContext())
        {
            await arrangeContext.Events.AddAsync(existingEvent, TestContext.Current.CancellationToken);
            await arrangeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        using var httpRequest = AuthTestHelper.CreateAuthorizedRequest(HttpMethod.Delete, $"/events/{existingEvent.Id}", admin.Token);
        var httpResponse = await _client.SendAsync(httpRequest, TestContext.Current.CancellationToken);

        await using var verifyContext = _factory.CreateContext();
        var exists = await verifyContext.Events.AsNoTracking().AnyAsync(e => e.Id == existingEvent.Id, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, httpResponse.StatusCode);
        Assert.False(exists);
    }

    [Fact]
    public async Task GET_ShouldReturnEventById()
    {
        await _factory.ResetDatabaseAsync();

        var existingEvent = new Event("Event by id", "Description for event by id", new DateTimeOffset(2026, 10, 13, 10, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 10, 13, 12, 0, 0, TimeSpan.Zero), 6);
        await using (var arrangeContext = _factory.CreateContext())
        {
            await arrangeContext.Events.AddAsync(existingEvent, TestContext.Current.CancellationToken);
            await arrangeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var httpResponse = await _client.GetAsync($"/events/{existingEvent.Id}", TestContext.Current.CancellationToken);
        var responseBody = await httpResponse.Content.ReadFromJsonAsync<EventDto>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        Assert.NotNull(responseBody);
        Assert.Equal(existingEvent.Id, responseBody.Id);
    }

    [Fact]
    public async Task GET_WithoutFiltersShouldReturnEvents()
    {
        await _factory.ResetDatabaseAsync();

        var events = new[]
        {
            new Event("First event", "Description for first event", new DateTimeOffset(2026, 10, 14, 10, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 10, 14, 12, 0, 0, TimeSpan.Zero), 5),
            new Event("Second event", "Description for second event", new DateTimeOffset(2026, 10, 15, 10, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 10, 15, 12, 0, 0, TimeSpan.Zero), 6),
            new Event("Third event", null, new DateTimeOffset(2026, 10, 16, 10, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 10, 16, 12, 0, 0, TimeSpan.Zero), 7)
        };
        await using (var arrangeContext = _factory.CreateContext())
        {
            await arrangeContext.Events.AddRangeAsync(events, TestContext.Current.CancellationToken);
            await arrangeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var httpResponse = await _client.GetAsync("/events", TestContext.Current.CancellationToken);
        var responseBody = await httpResponse.Content.ReadFromJsonAsync<PaginatedResult<EventDto>>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        Assert.NotNull(responseBody);
        Assert.Equal(3, responseBody.TotalItems);
    }

    [Fact]
    public async Task POST_Book_ShouldCreateBooking()
    {
        await _factory.ResetDatabaseAsync();

        var user = await AuthTestHelper.RegisterAndLoginAsync(_client, cancellationToken: TestContext.Current.CancellationToken);
        var existingEvent = new Event("Bookable event", "Description for bookable event", DateTimeOffset.UtcNow.AddDays(30), DateTimeOffset.UtcNow.AddDays(30).AddHours(2), 3);
        await using (var arrangeContext = _factory.CreateContext())
        {
            await arrangeContext.Events.AddAsync(existingEvent, TestContext.Current.CancellationToken);
            await arrangeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        using var httpRequest = AuthTestHelper.CreateAuthorizedRequest(HttpMethod.Post, $"/events/{existingEvent.Id}/book", user.Token);
        var httpResponse = await _client.SendAsync(httpRequest, TestContext.Current.CancellationToken);
        var responseBody = await httpResponse.Content.ReadFromJsonAsync<BookingResponse>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Accepted, httpResponse.StatusCode);
        Assert.NotNull(responseBody);
        await using var verifyContext = _factory.CreateContext();
        var savedBooking = await verifyContext.Bookings.AsNoTracking().SingleAsync(b => b.Id == responseBody.Id, TestContext.Current.CancellationToken);
        var savedEvent = await verifyContext.Events.AsNoTracking().SingleAsync(e => e.Id == existingEvent.Id, TestContext.Current.CancellationToken);
        Assert.Equal(BookingStatus.Pending, savedBooking.Status);
        Assert.Equal(2, savedEvent.AvailableSeats);
    }

    [Fact]
    public async Task POST_Book_WithoutToken_ShouldReturnUnauthorized()
    {
        await _factory.ResetDatabaseAsync();

        var existingEvent = new Event("Protected booking event", "Should require token", DateTimeOffset.UtcNow.AddDays(30), DateTimeOffset.UtcNow.AddDays(30).AddHours(2), 3);
        await using (var arrangeContext = _factory.CreateContext())
        {
            await arrangeContext.Events.AddAsync(existingEvent, TestContext.Current.CancellationToken);
            await arrangeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var response = await _client.PostAsync($"/events/{existingEvent.Id}/book", content: null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
