using BGA.Events.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BGA.Events.Infrastructure.DataAccess.Configurations;

public sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");

        builder.HasKey(@event => @event.Id);

        builder.Property(@event => @event.Id)
            .ValueGeneratedNever();

        builder.Property(@event => @event.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(@event => @event.Description)
            .HasMaxLength(1000);
    }
}
