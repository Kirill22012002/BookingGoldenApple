using System.Runtime.Serialization;

namespace BGA.Users.API.Extensions;

public static class EnumExtensions
{
    public static T GetEnumFromString<T>(this string value) where T : Enum
    {
        var type = typeof(T);
        foreach (var field in type.GetFields())
        {
            var attribute = Attribute.GetCustomAttribute(field, typeof(EnumMemberAttribute)) as EnumMemberAttribute;
            if (attribute?.Value == value)
            {
                return (T)field.GetValue(null)!;
            }
        }

        throw new ArgumentException($"Unknown value: {value}");
    }
}
