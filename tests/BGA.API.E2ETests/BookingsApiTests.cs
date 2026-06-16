using BGA.API.Dtos;
using BGA.API.E2ETests.Infrastructure;
using BGA.Domain.Models;
using BGA.Domain.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;

namespace BGA.API.E2ETests;

public class BookingsApiTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.HttpClient;
    private readonly CustomWebApplicationFactory _factory = factory;

    [Fact]
    public async Task GET_ShouldReturnBooking()
    {
        await _factory.ResetDatabaseAsync();

        // Arrange
        var existingEvent = new Event(
            title: "Event with booking",
            description: "Description for event with booking",
            startAt: new DateTimeOffset(2026, 1, 18, 10, 0, 0, TimeSpan.Zero),
            endAt: new DateTimeOffset(2026, 1, 18, 12, 0, 0, TimeSpan.Zero),
            totalSeats: 4);
        var existingBooking = new Booking(
            eventId: existingEvent.Id,
            status: BookingStatus.Pending,
            createdAt: new DateTimeOffset(2026, 1, 18, 9, 0, 0, TimeSpan.Zero));
        existingBooking.Confirm();

        await using (var arrangeContext = _factory.CreateContext())
        {
            await arrangeContext.Events.AddAsync(existingEvent, TestContext.Current.CancellationToken);
            await arrangeContext.Bookings.AddAsync(existingBooking, TestContext.Current.CancellationToken);
            await arrangeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act
        var httpResponse = await _client.GetAsync($"/bookings/{existingBooking.Id}", TestContext.Current.CancellationToken);
        var responseBody = await httpResponse.Content.ReadFromJsonAsync<BookingDto>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        Assert.NotNull(responseBody);

        await using var verifyContext = _factory.CreateContext();
        var saved = await verifyContext.Bookings
            .AsNoTracking()
            .SingleAsync(b => b.Id == existingBooking.Id, TestContext.Current.CancellationToken);

        Assert.Equal(saved.Id, responseBody.Id);
        Assert.Equal(saved.EventId, responseBody.EventId);
        Assert.Equal("confirmed", responseBody.Status);
        Assert.Equal(saved.CreatedAt, responseBody.CreatedAt);
        Assert.Equal(saved.ProcessedAt, responseBody.ProcessedAt);
    }
}
