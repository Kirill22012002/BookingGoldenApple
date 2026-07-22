namespace BGA.Events.Application.Caching;

public static class EventCacheKeys
{
    public const string Top10 = "events:top10";

    public static string GetById(Guid id)
    {
        return $"event:{id}";
    }
}
