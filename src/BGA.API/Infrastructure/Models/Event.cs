namespace BGA.API.Infrastructure.Models;

public class Event(string title, string? description, DateTimeOffset startAt, DateTimeOffset endAt, int totalSeats)
{
    public Guid Id { get; set; }
    public string Title { get; set; } = title;
    public string? Description { get; set; } = description;
    public DateTimeOffset StartAt { get; set; } = startAt;
    public DateTimeOffset EndAt { get; set; } = endAt;
    public int TotalSeats { get; private set; } = totalSeats;
    public int AvailableSeats { get; private set; } = totalSeats;

    public bool TryReserveSeats(int count = 1)
    {
        if (AvailableSeats < count) return false;

        AvailableSeats -= count;
        return true;
    }

    public void ReleaseSeats(int count = 1)
    {
        if (TotalSeats >= AvailableSeats + count) AvailableSeats += count;
        else AvailableSeats = TotalSeats;
    }
}
