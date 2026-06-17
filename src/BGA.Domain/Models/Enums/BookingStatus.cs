using System.Runtime.Serialization;

namespace BGA.Domain.Models.Enums;

public enum BookingStatus
{
    [EnumMember(Value = "pending")]
    Pending,

    [EnumMember(Value = "confirmed")]
    Confirmed,

    [EnumMember(Value = "rejected")]
    Rejected
}
