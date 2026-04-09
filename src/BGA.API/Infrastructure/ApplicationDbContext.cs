using BGA.API.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace BGA.API.Infrastructure;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Event> Events { get; set; }
    public DbSet<Booking> Bookings { get; set; }
}
