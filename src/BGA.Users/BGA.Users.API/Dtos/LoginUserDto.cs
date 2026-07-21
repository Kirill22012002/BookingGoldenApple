using BGA.Users.API.Attributes;

namespace BGA.Users.API.Dtos;

public sealed record LoginUserDto
{
    [FieldRequired]
    public required string Login { get; set; }

    [FieldRequired]
    public required string Password { get; set; }
}
