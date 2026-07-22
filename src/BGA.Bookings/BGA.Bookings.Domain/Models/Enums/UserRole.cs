using System.Runtime.Serialization;

namespace BGA.Bookings.Domain.Models.Enums;

public enum UserRole
{
    [EnumMember(Value = "user")]
    User,

    [EnumMember(Value = "admin")]
    Admin
}
