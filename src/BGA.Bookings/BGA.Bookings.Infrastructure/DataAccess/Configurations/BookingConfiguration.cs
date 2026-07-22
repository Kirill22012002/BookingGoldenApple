using BGA.Bookings.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BGA.Bookings.Infrastructure.DataAccess.Configurations;

public sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");

        builder.HasKey(booking => booking.Id);

        builder.Property(booking => booking.Id)
            .ValueGeneratedNever();

        builder.Property(booking => booking.EventId)
            .IsRequired();

        builder.Property(booking => booking.UserId)
            .IsRequired();

        builder.Property(booking => booking.Status)
            .HasConversion<string>();
    }
}
