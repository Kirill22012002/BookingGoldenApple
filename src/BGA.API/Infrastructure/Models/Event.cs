using BGA.API.Application.Exceptions;

namespace BGA.API.Infrastructure.Models;

public class Event
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset StartAt { get; private set; }
    public DateTimeOffset EndAt { get; private set; }
    public int TotalSeats { get; private set; }
    public int AvailableSeats { get; private set; }

    private readonly object _lock = new();

    public Event(string title, string? description, DateTimeOffset startAt, DateTimeOffset endAt, int totalSeats) // TODO: add unit test for all cases
    {
        ValidateStartAtAndEndAt(startAt, endAt);
        ValidateTotalSeats(totalSeats);

        Title = title;
        Description = description;
        StartAt = startAt;
        EndAt = endAt;
        TotalSeats = totalSeats;
        AvailableSeats = totalSeats;
    }

    public void Reschedule(DateTimeOffset newStartAt, DateTimeOffset newEndAt)
    {
        ValidateStartAtAndEndAt(newStartAt, newEndAt);

        StartAt = newStartAt;
        EndAt = newEndAt;
    }

    public bool TryReserveSeats(int count = 1)
    {
        lock (_lock)
        {
            if (AvailableSeats < count)
                return false;

            AvailableSeats -= count;
            return true;
        }
    }

    public void ReleaseSeats(int count = 1)
    {
        lock (_lock)
        {
            if (TotalSeats >= AvailableSeats + count)
                AvailableSeats += count;
            else
                AvailableSeats = TotalSeats;
        }
    }

    private static void ValidateStartAtAndEndAt(DateTimeOffset startAt, DateTimeOffset endAt)
    {
        if (startAt == default) throw new ValidationException(nameof(startAt), $"{nameof(startAt)} must be valid value (not default value)");
        if (endAt == default) throw new ValidationException(nameof(endAt), $"{nameof(endAt)} must be valid value (not default value)");
        if (startAt.CompareTo(endAt) >= 0) throw new ValidationException(nameof(endAt), $"{nameof(endAt)} must be greater than the {nameof(startAt)}");
    }

    private static void ValidateTotalSeats(int totalSeats)
    {
        if (totalSeats <= 0) throw new ValidationException(nameof(totalSeats), $"{nameof(totalSeats)} must be valid value (not default value)");
    }
}
