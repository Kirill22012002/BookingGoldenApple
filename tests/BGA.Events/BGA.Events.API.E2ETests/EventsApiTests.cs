using BGA.Events.API.Dtos;
using BGA.Events.API.E2ETests.Infrastructure;
using BGA.Events.API.E2ETests.Models;
using BGA.Events.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;

namespace BGA.Events.API.E2ETests;

public class EventsApiTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.HttpClient;
    private readonly CustomWebApplicationFactory _factory = factory;

    [Fact]
    public async Task POST_ShouldCreateEvent()
    {
        await _factory.ResetDatabaseAsync();

        var admin = AuthTestHelper.CreateSession("Admin");
        var request = new AddEventDto
        {
            Title = "Cycling",
            Description = "Cycling with my best friends",
            StartAt = new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero),
            EndAt = new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero),
            TotalSeats = 4
        };

        using var httpRequest = AuthTestHelper.CreateAuthorizedRequest(HttpMethod.Post, "/events", admin.Token, request);
        var response = await _client.SendAsync(httpRequest, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<EventResponse>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);

        await using var verifyContext = _factory.CreateContext();
        var saved = await verifyContext.Events.AsNoTracking().SingleAsync(@event => @event.Id == body.Id, TestContext.Current.CancellationToken);
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
            StartAt = new DateTimeOffset(2026, 8, 2, 10, 0, 0, TimeSpan.Zero),
            EndAt = new DateTimeOffset(2026, 8, 2, 12, 0, 0, TimeSpan.Zero),
            TotalSeats = 10
        };

        var response = await _client.PostAsJsonAsync("/events", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task POST_WithUserToken_ShouldReturnForbidden()
    {
        await _factory.ResetDatabaseAsync();

        var user = AuthTestHelper.CreateSession();
        var request = new AddEventDto
        {
            Title = "Admin only event",
            Description = "Should reject regular users",
            StartAt = new DateTimeOffset(2026, 8, 3, 10, 0, 0, TimeSpan.Zero),
            EndAt = new DateTimeOffset(2026, 8, 3, 12, 0, 0, TimeSpan.Zero),
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

        var admin = AuthTestHelper.CreateSession("Admin");
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
        var response = await _client.SendAsync(httpRequest, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await using var verifyContext = _factory.CreateContext();
        var saved = await verifyContext.Events.AsNoTracking().SingleAsync(@event => @event.Id == existingEvent.Id, TestContext.Current.CancellationToken);
        Assert.Equal("Updated event", saved.Title);
        Assert.Equal(request.StartAt, saved.StartAt);
    }

    [Fact]
    public async Task DELETE_ShouldDeleteEvent()
    {
        await _factory.ResetDatabaseAsync();

        var admin = AuthTestHelper.CreateSession("Admin");
        var existingEvent = new Event("Event to delete", "Description", new DateTimeOffset(2026, 10, 12, 10, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 10, 12, 12, 0, 0, TimeSpan.Zero), 8);
        await using (var arrangeContext = _factory.CreateContext())
        {
            await arrangeContext.Events.AddAsync(existingEvent, TestContext.Current.CancellationToken);
            await arrangeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        using var httpRequest = AuthTestHelper.CreateAuthorizedRequest(HttpMethod.Delete, $"/events/{existingEvent.Id}", admin.Token);
        var response = await _client.SendAsync(httpRequest, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await using var verifyContext = _factory.CreateContext();
        var exists = await verifyContext.Events.AsNoTracking().AnyAsync(@event => @event.Id == existingEvent.Id, TestContext.Current.CancellationToken);
        Assert.False(exists);
    }

    [Fact]
    public async Task GET_ShouldReturnEventById()
    {
        await _factory.ResetDatabaseAsync();

        var existingEvent = new Event("Event by id", "Description", new DateTimeOffset(2026, 10, 13, 10, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 10, 13, 12, 0, 0, TimeSpan.Zero), 6);
        await using (var arrangeContext = _factory.CreateContext())
        {
            await arrangeContext.Events.AddAsync(existingEvent, TestContext.Current.CancellationToken);
            await arrangeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var response = await _client.GetAsync($"/events/{existingEvent.Id}", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<EventDto>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(existingEvent.Id, body.Id);
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

        var response = await _client.GetAsync("/events", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<PaginatedResult<EventDto>>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(3, body.TotalItems);
    }
}
