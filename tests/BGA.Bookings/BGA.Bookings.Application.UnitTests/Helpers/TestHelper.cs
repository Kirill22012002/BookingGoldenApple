namespace BGA.Bookings.Application.UnitTests.Helpers;

public static class TestHelper
{
    public static DateTimeOffset Yesterday
        => new(2026, 07, 14, 12, 0, 0, TimeSpan.Zero);

    public static DateTimeOffset Now
        => new(2026, 07, 15, 12, 0, 0, TimeSpan.Zero);

    public static DateTimeOffset Tomorrow
        => new(2026, 07, 16, 12, 0, 0, TimeSpan.Zero);
}

