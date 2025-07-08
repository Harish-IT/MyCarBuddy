using System;

namespace MyCarBuddy.API.Models
{
    public class Bookings
    {
        public int BookingID { get; set; }
        public int CustID { get; set; }
        public int TechID { get; set; }
        public int VehicleID { get; set; }
        public int PricingID { get; set; }
        public int AddressID { get; set; }
        public DateTime ScheduledDate { get; set; }
        public decimal BookingPrice { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
        public string OTPForCompletion { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
