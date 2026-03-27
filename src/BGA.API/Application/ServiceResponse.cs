namespace BGA.API.Application;

public class ServiceResponse
{
    public bool Succeeded { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = [];

    public static ServiceResponse Success()
        => new() { Succeeded = true, Message = "Operation successful" };

    public static ServiceResponse Failure(List<string> errors)
        => new() { Succeeded = false, Errors = errors, Message = "Operation failed" };
}

public class ServiceResponse<T> : ServiceResponse
{
    public T? Data { get; set; }

    public static ServiceResponse<T> Success(T data)
        => new() { Data = data, Succeeded = true, Message = "Operation successful" };

    public static new ServiceResponse<T> Failure(List<string> errors)
        => new() { Succeeded = false, Errors = errors, Message = "Operation failed" };
}

public record PaginatedResult<T>
{
    public required IEnumerable<T> Items { get; set; }
    public required int TotalItems { get; set; }
    public required int PageNumber { get; set; }
    public required int PageSize { get; set; }
}
