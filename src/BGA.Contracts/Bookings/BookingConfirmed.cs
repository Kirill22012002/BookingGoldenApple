namespace BGA.Contracts.Bookings;

public sealed record BookingConfirmed(
    Guid BookingId,
    Guid EventId,
    Guid UserId,
    int SeatsCount,
    DateTimeOffset ConfirmedAt);
