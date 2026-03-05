using System.Net;

namespace BGA.API;

public class ApiResult<T> : ApiBaseResult
{
    public required T Data { get; set; }
}

public class ApiResult : ApiBaseResult { }

public abstract class ApiBaseResult
{
    public required bool Success { get; set; }
    public required HttpStatusCode StatusCode { get; set; }
    public DateTime DateTime { get; set; } = DateTime.UtcNow;
    public required string Message { get; set; }
}
