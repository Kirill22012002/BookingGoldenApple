namespace BGA.API.Infrastructure.Models;

public class Event
{
    public Event(string title, string? description, DateTimeOffset startAt, DateTimeOffset endAt, int totalSeats)
    {
        Title = title;
        Description = description;
        StartAt = startAt;
        EndAt = endAt;
        TotalSeats = totalSeats;
        AvailableSeats = totalSeats;
    }

    public Event(Guid id, string title, string? description, DateTimeOffset startAt, DateTimeOffset endAt, int totalSeats)
        : this(title, description, startAt, endAt, totalSeats)
    {
        Id = id;
    }

    public Guid Id { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }

    public bool TryReserveSeats(int count = 1)
    {
        if (AvailableSeats >= count)
        {
            AvailableSeats -= count;
            return true;
        }
        return false;
    }

    public void ReleaseSeats(int count = 1)
    {
        AvailableSeats += count;
    }
}
