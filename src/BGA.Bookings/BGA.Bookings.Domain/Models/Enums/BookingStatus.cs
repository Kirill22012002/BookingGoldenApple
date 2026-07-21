using System.Runtime.Serialization;

namespace BGA.Bookings.Domain.Models.Enums;

public enum BookingStatus
{
    [EnumMember(Value = "pending")]
    Pending,

    [EnumMember(Value = "confirmed")]
    Confirmed,

    [EnumMember(Value = "rejected")]
    Rejected,

    [EnumMember(Value = "cancelled")]
    Cancelled
}
