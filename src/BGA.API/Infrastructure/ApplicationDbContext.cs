using BGA.API.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace BGA.API.Infrastructure;

public class ApplicationDbContext : DbContext
{
    public DbSet<Event> Events { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseInMemoryDatabase("DatabaseInMemory");
    }
}
