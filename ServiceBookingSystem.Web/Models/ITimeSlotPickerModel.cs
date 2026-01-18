using System.ComponentModel.DataAnnotations;

namespace ServiceBookingSystem.Web.Models;

public interface ITimeSlotPickerModel
{
    [DataType(DataType.Date)]
    DateTime Date { get; set; }

    TimeSpan Time { get; set; }
}