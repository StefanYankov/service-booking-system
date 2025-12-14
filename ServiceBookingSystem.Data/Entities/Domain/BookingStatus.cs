using System.ComponentModel;

namespace ServiceBookingSystem.Data.Entities.Domain;

public enum BookingStatus
{

    [Description("Pending Approval")]
    Pending = 1,

    [Description("Confirmed")]
    Confirmed = 2,

    [Description("Declined")]
    Declined = 3,

    [Description("Cancelled by Customer")]
    Cancelled = 4,

    [Description("Completed")]
    Completed = 5
}