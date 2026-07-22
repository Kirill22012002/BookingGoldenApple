using BGA.Events.Domain.Exceptions;
using System.Text.Json.Serialization;

namespace BGA.Events.Domain.Models;

public class Event
{
    [JsonInclude]
    public Guid Id { get; private set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    [JsonInclude]
    public DateTimeOffset StartAt { get; private set; }
    [JsonInclude]
    public DateTimeOffset EndAt { get; private set; }
    [JsonInclude]
    public int TotalSeats { get; private set; }
    [JsonInclude]
    public int AvailableSeats { get; private set; }

    private readonly object _lock = new();

    private Event()
    {
    }

    public Event(string title, string? description, DateTimeOffset startAt, DateTimeOffset endAt, int totalSeats)
    {
        ValidateStartAtAndEndAt(startAt, endAt);
        ValidateTotalSeats(totalSeats);

        Id = Guid.NewGuid();
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
            {
                return false;
            }

            AvailableSeats -= count;
            return true;
        }
    }

    public void ReleaseSeats(int count = 1)
    {
        lock (_lock)
        {
            if (TotalSeats >= AvailableSeats + count)
            {
                AvailableSeats += count;
            }
            else
            {
                AvailableSeats = TotalSeats;
            }
        }
    }

    private static void ValidateStartAtAndEndAt(DateTimeOffset startAt, DateTimeOffset endAt)
    {
        if (startAt == default)
        {
            throw new ValidationException(nameof(startAt), $"{nameof(startAt)} must be valid value (not default value)");
        }

        if (endAt == default)
        {
            throw new ValidationException(nameof(endAt), $"{nameof(endAt)} must be valid value (not default value)");
        }

        if (startAt.CompareTo(endAt) >= 0)
        {
            throw new ValidationException(nameof(endAt), $"{nameof(endAt)} must be greater than the {nameof(startAt)}");
        }
    }

    private static void ValidateTotalSeats(int totalSeats)
    {
        if (totalSeats <= 0)
        {
            throw new ValidationException(nameof(totalSeats), $"{nameof(totalSeats)} must be valid value (not default value)");
        }
    }
}
