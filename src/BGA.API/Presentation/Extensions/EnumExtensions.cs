using System.Runtime.Serialization;

namespace BGA.API.Presentation.Extensions;

public static class EnumExtensions
{
    public static string GetEnumValue<T>(this T enumValue) where T : Enum
    {
        var type = enumValue.GetType();
        var memberInfo = type.GetMember(enumValue.ToString());
        var attributes = memberInfo[0].GetCustomAttributes(typeof(EnumMemberAttribute), false);
        return ((EnumMemberAttribute)attributes[0]).Value!;
    }

    public static T GetEnumFromString<T>(this string value) where T : Enum
    {
        var type = typeof(T);
        foreach (var field in type.GetFields())
        {
            var attribute = Attribute.GetCustomAttribute(field, typeof(EnumMemberAttribute)) as EnumMemberAttribute;
            if (attribute != null && attribute.Value == value)
            {
                return (T)field.GetValue(null)!;
            }
        }

        throw new ArgumentException($"Unknown value: {value}");
    }
}
