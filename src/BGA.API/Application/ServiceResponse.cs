namespace BGA.API.Application;

public class ServiceResponse
{
    public bool Succeeded { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = [];
    public ServiceErrorType ErrorType { get; set; }
    public Exception? Exception { get; set; }
    public Dictionary<string, string> ValidationErrors { get; set; } = [];

    public static ServiceResponse Success()
        => new() { Succeeded = true, Message = "Operation successful" };

    public static ServiceResponse Failure(string error, ServiceErrorType errorType)
        => new() { Succeeded = false, Errors = [error], Message = "Operation failed", ErrorType = errorType };

    public static ServiceResponse Failure(Exception ex, string error)
        => new() { Succeeded = false, Exception = ex, Errors = [error], Message = "Operation failed" };

    public static ServiceResponse Failure(Dictionary<string, string> validationErrors)
        => new() { Succeeded = false, ValidationErrors = validationErrors, Message = "Operation failed", ErrorType = ServiceErrorType.Validation };
}

public class ServiceResponse<T> : ServiceResponse
{
    public T? Data { get; set; }

    public static ServiceResponse<T> Success(T data)
        => new() { Data = data, Succeeded = true, Message = "Operation successful" };

    public static new ServiceResponse<T> Failure(string error, ServiceErrorType errorType)
        => new() { Succeeded = false, Errors = [error], Message = "Operation failed", ErrorType = errorType };

    public static new ServiceResponse<T> Failure(Exception ex, string error)
        => new() { Succeeded = false, Exception = ex, Errors = [error], Message = "Operation failed" };

    public static new ServiceResponse<T> Failure(Dictionary<string, string> validationErrors)
        => new() { Succeeded = false, ValidationErrors = validationErrors, Message = "Operation failed", ErrorType = ServiceErrorType.Validation };
}

public record PaginatedResult<T>
{
    public required IEnumerable<T> Items { get; set; }
    public required int TotalItems { get; set; }
    public required int PageNumber { get; set; }
    public required int PageSize { get; set; }
}

public enum ServiceErrorType
{
    None,
    Validation,
    NotFound,
    InternalProblem
}
