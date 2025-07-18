using Microsoft.VisualBasic;

namespace MyCarBuddy.API.Models
{
    public class PaymentsModel
    {
        public int? PaymentID { get; set; }
        public int BookingID {  get; set; }
        public decimal AmountPaid {  get; set; }
        public string PaymentMode {  get; set; }
         public string TransactionID {  get; set; }
        public bool? IsRefunded {  get; set; }
    }
}
