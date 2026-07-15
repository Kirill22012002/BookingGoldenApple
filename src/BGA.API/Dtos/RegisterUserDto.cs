using BGA.API.Attributes;

namespace BGA.API.Dtos;

public sealed record RegisterUserDto
{
    [FieldRequired]
    public required string Login { get; set; }

    [FieldRequired]
    public required string Password { get; set; }

    public string? Role { get; set; }
}
