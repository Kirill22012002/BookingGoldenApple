using BGA.Bookings.API.Extensions;
using BGA.Bookings.Application.Services.Interfaces;
using BGA.Bookings.Domain.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BGA.Bookings.API.Controllers;

[Authorize]
[ApiController]
[Route("bookings")]
public sealed class BookingsController(IBookingService bookingService) : ControllerBase
{
    [HttpPost("/events/{eventId:guid}/book")]
    public async Task<IActionResult> Create([FromRoute] Guid eventId, CancellationToken cancellationToken)
    {
        var response = await bookingService.CreateBookingAsync(eventId, User.GetRequiredUserId(), cancellationToken);
        var responseDto = response.MapToDto();

        return AcceptedAtAction(
            nameof(Get),
            new { id = responseDto.Id },
            new { id = responseDto.Id, eventId = responseDto.EventId, status = responseDto.Status });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var response = await bookingService.GetBookingByIdAsync(id, cancellationToken);
        return Ok(response.MapToDto());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var userRole = User.IsInRole(UserRole.Admin.ToString()) ? UserRole.Admin : UserRole.User;
        await bookingService.CancelBookingAsync(id, User.GetRequiredUserId(), userRole, cancellationToken);
        return NoContent();
    }
}
