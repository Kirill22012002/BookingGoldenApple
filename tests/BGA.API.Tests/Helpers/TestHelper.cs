using BGA.API.Infrastructure.Models;

namespace BGA.API.Tests.Helpers;

public class TestHelper
{
    public static DateTimeOffset Yesterday
        => DateTimeOffset.UtcNow.AddDays(-1);

    public static DateTimeOffset Tomorrow
        => DateTimeOffset.UtcNow.AddDays(1);
}
