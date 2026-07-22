using BGA.Users.API.Extensions;
using BGA.Users.Domain.Models.Enums;

namespace BGA.Users.API.UnitTests;

public class UserRoleExtensionsTests
{
    [Theory]
    [InlineData("user", UserRole.User)]
    [InlineData("admin", UserRole.Admin)]
    public void GetEnumFromString_WithEnumMemberAttribute_ReturnsCorrectEnum(string role, UserRole expectedResult)
    {
        var result = role.GetEnumFromString<UserRole>();

        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void GetEnumFromString_WithUnknownValue_ThrowsArgumentException()
    {
        Action action = () => _ = "manager".GetEnumFromString<UserRole>();

        Assert.Throws<ArgumentException>(action);
    }
}
