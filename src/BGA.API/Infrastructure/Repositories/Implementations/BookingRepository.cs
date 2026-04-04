using BGA.API.Infrastructure.Repositories.Interfaces;

namespace BGA.API.Infrastructure.Repositories.Implementations;

public class BookingRepository(ApplicationDbContext _dbContext) : IBookingRepository
{

}
