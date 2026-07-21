using BGA.API.Dtos;
using BGA.API.E2ETests.Infrastructure;
using BGA.API.E2ETests.Models;
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

        var user = await AuthTestHelper.RegisterAndLoginAsync(_client, cancellationToken: TestContext.Current.CancellationToken);
        var existingEvent = new Event("Event with booking", "Description for event with booking", DateTimeOffset.UtcNow.AddDays(20), DateTimeOffset.UtcNow.AddDays(20).AddHours(2), 4);
        await using (var arrangeContext = _factory.CreateContext())
        {
            await arrangeContext.Events.AddAsync(existingEvent, TestContext.Current.CancellationToken);
            await arrangeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        using var createRequest = AuthTestHelper.CreateAuthorizedRequest(HttpMethod.Post, $"/events/{existingEvent.Id}/book", user.Token);
        var createResponse = await _client.SendAsync(createRequest, TestContext.Current.CancellationToken);
        var createdBooking = await createResponse.Content.ReadFromJsonAsync<BookingResponse>(cancellationToken: TestContext.Current.CancellationToken);

        using var getRequest = AuthTestHelper.CreateAuthorizedRequest(HttpMethod.Get, $"/bookings/{createdBooking!.Id}", user.Token);
        var httpResponse = await _client.SendAsync(getRequest, TestContext.Current.CancellationToken);
        var responseBody = await httpResponse.Content.ReadFromJsonAsync<BookingDto>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        Assert.NotNull(responseBody);

        await using var verifyContext = _factory.CreateContext();
        var saved = await verifyContext.Bookings.AsNoTracking().SingleAsync(b => b.Id == createdBooking.Id, TestContext.Current.CancellationToken);
        Assert.Equal(saved.Id, responseBody.Id);
        Assert.Equal(saved.EventId, responseBody.EventId);
        Assert.Equal("pending", responseBody.Status);
    }

    [Fact]
    public async Task GET_WithoutToken_ShouldReturnUnauthorized()
    {
        await _factory.ResetDatabaseAsync();

        var user = await AuthTestHelper.RegisterAndLoginAsync(_client, cancellationToken: TestContext.Current.CancellationToken);
        var existingEvent = new Event("Event with protected booking", "Description", DateTimeOffset.UtcNow.AddDays(20), DateTimeOffset.UtcNow.AddDays(20).AddHours(2), 4);
        await using (var arrangeContext = _factory.CreateContext())
        {
            await arrangeContext.Events.AddAsync(existingEvent, TestContext.Current.CancellationToken);
            await arrangeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        using var createRequest = AuthTestHelper.CreateAuthorizedRequest(HttpMethod.Post, $"/events/{existingEvent.Id}/book", user.Token);
        var createResponse = await _client.SendAsync(createRequest, TestContext.Current.CancellationToken);
        var createdBooking = await createResponse.Content.ReadFromJsonAsync<BookingResponse>(cancellationToken: TestContext.Current.CancellationToken);

        var response = await _client.GetAsync($"/bookings/{createdBooking!.Id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DELETE_WithoutToken_ShouldReturnUnauthorized()
    {
        await _factory.ResetDatabaseAsync();

        var user = await AuthTestHelper.RegisterAndLoginAsync(_client, cancellationToken: TestContext.Current.CancellationToken);
        var existingEvent = new Event("Event with cancellable booking", "Description", DateTimeOffset.UtcNow.AddDays(20), DateTimeOffset.UtcNow.AddDays(20).AddHours(2), 4);
        await using (var arrangeContext = _factory.CreateContext())
        {
            await arrangeContext.Events.AddAsync(existingEvent, TestContext.Current.CancellationToken);
            await arrangeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        using var createRequest = AuthTestHelper.CreateAuthorizedRequest(HttpMethod.Post, $"/events/{existingEvent.Id}/book", user.Token);
        var createResponse = await _client.SendAsync(createRequest, TestContext.Current.CancellationToken);
        var createdBooking = await createResponse.Content.ReadFromJsonAsync<BookingResponse>(cancellationToken: TestContext.Current.CancellationToken);

        var response = await _client.DeleteAsync($"/bookings/{createdBooking!.Id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DELETE_WhenUserCancelsAnotherUsersBooking_ShouldReturnForbidden()
    {
        await _factory.ResetDatabaseAsync();

        var owner = await AuthTestHelper.RegisterAndLoginAsync(_client, cancellationToken: TestContext.Current.CancellationToken);
        var anotherUser = await AuthTestHelper.RegisterAndLoginAsync(_client, cancellationToken: TestContext.Current.CancellationToken);
        var existingEvent = new Event("Event with another users booking", "Description", DateTimeOffset.UtcNow.AddDays(20), DateTimeOffset.UtcNow.AddDays(20).AddHours(2), 4);
        await using (var arrangeContext = _factory.CreateContext())
        {
            await arrangeContext.Events.AddAsync(existingEvent, TestContext.Current.CancellationToken);
            await arrangeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        using var createRequest = AuthTestHelper.CreateAuthorizedRequest(HttpMethod.Post, $"/events/{existingEvent.Id}/book", owner.Token);
        var createResponse = await _client.SendAsync(createRequest, TestContext.Current.CancellationToken);
        var createdBooking = await createResponse.Content.ReadFromJsonAsync<BookingResponse>(cancellationToken: TestContext.Current.CancellationToken);

        using var deleteRequest = AuthTestHelper.CreateAuthorizedRequest(HttpMethod.Delete, $"/bookings/{createdBooking!.Id}", anotherUser.Token);
        var response = await _client.SendAsync(deleteRequest, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        await using var verifyContext = _factory.CreateContext();
        var savedBooking = await verifyContext.Bookings.AsNoTracking().SingleAsync(b => b.Id == createdBooking.Id, TestContext.Current.CancellationToken);
        Assert.Equal(BookingStatus.Pending, savedBooking.Status);
    }
}
