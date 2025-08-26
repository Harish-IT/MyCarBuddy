using System;

namespace MyCarBuddy.API.Models
{
    public class TimeSlotModel
    {
        public int? TsID { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public bool? Status { get; set; }
    }
}
