using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace MyCarBuddy.API.Models
{
    public class Bookings
    {
        public int CustID { get; set; }
        public int VehicleID { get; set; }
        public int PricingID { get; set; }
        public int AddressID { get; set; }
        public DateTime ScheduledDate { get; set; }
        public decimal BookingPrice { get; set; }
        public string Notes { get; set; }
        public string OTPForCompletion { get; set; }
        public int CouponID { get; set; }
        public List<IFormFile> Images { get; set; }
    }
}
