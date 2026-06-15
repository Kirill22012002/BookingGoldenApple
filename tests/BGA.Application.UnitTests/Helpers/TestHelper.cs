namespace BGA.Application.UnitTests.Helpers;

public class TestHelper
{
    public static DateTimeOffset Yesterday
        => DateTimeOffset.UtcNow.AddDays(-1);

    public static DateTimeOffset Now
        => DateTimeOffset.UtcNow;

    public static DateTimeOffset Tomorrow
        => DateTimeOffset.UtcNow.AddDays(1);
}
