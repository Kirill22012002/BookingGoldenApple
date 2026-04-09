using Microsoft.AspNetCore.Mvc;
using BGA.API.Presentation.Extensions;
using BGA.API.Application.Services.Interfaces;

namespace BGA.API.Presentation.Controllers;

[ApiController]
[Route("[controller]")]
public class BookingsController(IBookingService _bookingService) : BaseController
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var response = await _bookingService.GetBookingByIdAsync(id, cancellationToken);

        return response.Succeeded
            ? Ok(response?.Data?.MapToDto())
            : ProblemResponse(response);
    }
}
