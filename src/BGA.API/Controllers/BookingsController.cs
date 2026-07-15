using BGA.API.Extensions;
using BGA.Application.Services.Interfaces;
using BGA.Domain.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BGA.API.Controllers;

[Authorize]
[ApiController]
[Route("bookings")]
public sealed class BookingsController(IBookingService _bookingService) : BaseController
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var response = await _bookingService.GetBookingByIdAsync(id, cancellationToken);
        return Ok(response.MapToDto());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var userRole = User.IsInRole(UserRole.Admin.ToString()) ? UserRole.Admin : UserRole.User;
        await _bookingService.CancelBookingAsync(id, User.GetRequiredUserId(), userRole, cancellationToken);
        return NoContent();
    }
}
