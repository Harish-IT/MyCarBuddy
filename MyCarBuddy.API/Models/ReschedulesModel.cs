using System;

namespace MyCarBuddy.API.Models
{
    public class ReschedulesModel
    {
        public int? BookingID { get; set; }
        public DateTime OldSchedule {  get; set; }
        public DateTime NewSchedule { get; set; }

        public string Reason { get; set; } = string.Empty;
        public int? RequestedBy {  get; set; }
        public string Status { get; set; }


    }
}
