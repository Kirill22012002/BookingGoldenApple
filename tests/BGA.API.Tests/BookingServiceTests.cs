using BGA.API.Application.Services.Implementations;
using BGA.API.Infrastructure.Repositories.Interfaces;
using Moq;

namespace BGA.API.Tests;

public class BookingServiceTests
{
    private readonly Mock<IBookingRepository> _bookingRepository;
    private readonly Mock<IEventRepository> _eventRepository;
    private readonly BookingService _service;

    public BookingServiceTests()
    {
        _bookingRepository = new Mock<IBookingRepository>();
        _eventRepository = new Mock<IEventRepository>();
        _service = new BookingService(_bookingRepository.Object, _eventRepository.Object);
    }
}
