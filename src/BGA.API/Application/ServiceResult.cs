namespace BGA.API.Application;

public class ServiceBaseResult
{
    public required bool Success { get; set; }
    public string? ErrorMessage { get; set; }    
}

public class ServiceResult : ServiceBaseResult { }

public class ServiceResult<T> : ServiceResult
{
    public required T Data { get; set; }
}
