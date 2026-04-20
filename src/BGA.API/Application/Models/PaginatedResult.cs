namespace BGA.API.Application.Models;

public record PaginatedResult<T>
{
    public required IEnumerable<T> Items { get; set; }
    public required int TotalItems { get; set; }
    public required int PageNumber { get; set; }
    public required int PageSize { get; set; }
}
