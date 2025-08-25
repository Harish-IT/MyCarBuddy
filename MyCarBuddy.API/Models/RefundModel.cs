namespace MyCarBuddy.API.Models
{
    public class RefundModel
    {
        public int? BookingID { get; set; }
        public decimal Amount {  get; set; }
        public string RefundMethod {  get; set; }
        public string TransactionRef {  get; set; }
        public string Status { get; set; }
    }
}
