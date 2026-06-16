using BGA.API.Dtos;
using BGA.API.E2ETests.Infrastructure;
using BGA.API.E2ETests.Models;
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
            .SingleAsync(e => e.Id == responseBody.Id, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, httpResponse.StatusCode);
        Assert.Equal("Cycling", saved.Title);
        Assert.Equal("Cycling with my best friends", saved.Description);
        Assert.Equal(4, saved.TotalSeats);
    }
}
