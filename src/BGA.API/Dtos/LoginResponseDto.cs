namespace BGA.API.Dtos;

public sealed record LoginResponseDto
{
    public required string Token { get; set; }
}
