using Microsoft.VisualBasic;
using System;

namespace MyCarBuddy.API.Models
{

    // insert Model
    public class CancellationsModel
    {
        public int? BookingID {  get; set; }

        public  string CancelledBy { get; set; }
        public string Reason {  get; set; }
        public string RefundStatus {  get; set; }
    }
    //Get List Model
    public class CancellationList
    {
        public int CancelID { get; set; }
        public  int BookingID { get; set; }
        public string CancelledBy { get; set; }
        public string Reason { get; set; }
        public DateTime CancelledAt { get; set; }
        public string RefundStatus { get; set; }


    }
}
