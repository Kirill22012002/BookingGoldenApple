using BGA.Bookings.API.Dtos;
using BGA.Bookings.API.E2ETests.Infrastructure;
using BGA.Bookings.API.E2ETests.Models;
using BGA.Bookings.Domain.Models;
using BGA.Bookings.Domain.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;

namespace BGA.Bookings.API.E2ETests;

public class BookingsApiTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.HttpClient;
    private readonly CustomWebApplicationFactory _factory = factory;

    [Fact]
    public async Task POST_ShouldCreateBooking()
    {
        await _factory.ResetDatabaseAsync();

        var user = AuthTestHelper.CreateSession();
        var eventId = Guid.NewGuid();

        using var request = AuthTestHelper.CreateAuthorizedRequest(HttpMethod.Post, $"/events/{eventId}/book", user.Token);
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<BookingResponse>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(eventId, body.EventId);
        Assert.Equal("pending", body.Status);

        await using var verifyContext = _factory.CreateContext();
        var saved = await verifyContext.Bookings.AsNoTracking().SingleAsync(booking => booking.Id == body.Id, TestContext.Current.CancellationToken);
        Assert.Equal(user.UserId, saved.UserId);
        Assert.Equal(BookingStatus.Pending, saved.Status);
    }

    [Fact]
    public async Task GET_ShouldReturnBooking()
    {
        await _factory.ResetDatabaseAsync();

        var user = AuthTestHelper.CreateSession();
        var booking = new Booking(Guid.NewGuid(), user.UserId, BookingStatus.Pending, DateTimeOffset.UtcNow);
        await using (var arrangeContext = _factory.CreateContext())
        {
            await arrangeContext.Bookings.AddAsync(booking, TestContext.Current.CancellationToken);
            await arrangeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        using var request = AuthTestHelper.CreateAuthorizedRequest(HttpMethod.Get, $"/bookings/{booking.Id}", user.Token);
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<BookingDto>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(booking.Id, body.Id);
        Assert.Equal(booking.EventId, body.EventId);
        Assert.Equal("pending", body.Status);
    }

    [Fact]
    public async Task GET_WithoutToken_ShouldReturnUnauthorized()
    {
        await _factory.ResetDatabaseAsync();

        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid(), BookingStatus.Pending, DateTimeOffset.UtcNow);
        await using (var arrangeContext = _factory.CreateContext())
        {
            await arrangeContext.Bookings.AddAsync(booking, TestContext.Current.CancellationToken);
            await arrangeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var response = await _client.GetAsync($"/bookings/{booking.Id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DELETE_WithoutToken_ShouldReturnUnauthorized()
    {
        await _factory.ResetDatabaseAsync();

        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid(), BookingStatus.Pending, DateTimeOffset.UtcNow);
        await using (var arrangeContext = _factory.CreateContext())
        {
            await arrangeContext.Bookings.AddAsync(booking, TestContext.Current.CancellationToken);
            await arrangeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var response = await _client.DeleteAsync($"/bookings/{booking.Id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DELETE_WhenUserCancelsAnotherUsersBooking_ShouldReturnForbidden()
    {
        await _factory.ResetDatabaseAsync();

        var owner = AuthTestHelper.CreateSession();
        var anotherUser = AuthTestHelper.CreateSession();
        var booking = new Booking(Guid.NewGuid(), owner.UserId, BookingStatus.Pending, DateTimeOffset.UtcNow);
        await using (var arrangeContext = _factory.CreateContext())
        {
            await arrangeContext.Bookings.AddAsync(booking, TestContext.Current.CancellationToken);
            await arrangeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        using var request = AuthTestHelper.CreateAuthorizedRequest(HttpMethod.Delete, $"/bookings/{booking.Id}", anotherUser.Token);
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        await using var verifyContext = _factory.CreateContext();
        var saved = await verifyContext.Bookings.AsNoTracking().SingleAsync(entity => entity.Id == booking.Id, TestContext.Current.CancellationToken);
        Assert.Equal(BookingStatus.Pending, saved.Status);
    }
}
