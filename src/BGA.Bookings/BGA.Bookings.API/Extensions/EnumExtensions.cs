using System.Runtime.Serialization;

namespace BGA.Bookings.API.Extensions;

public static class EnumExtensions
{
    public static string GetEnumValue<T>(this T enumValue) where T : Enum
    {
        var type = enumValue.GetType();
        var memberInfo = type.GetMember(enumValue.ToString());
        var attributes = memberInfo[0].GetCustomAttributes(typeof(EnumMemberAttribute), false);
        return ((EnumMemberAttribute)attributes[0]).Value!;
    }
}
