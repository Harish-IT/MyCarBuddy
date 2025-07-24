namespace MyCarBuddy.API.Models
{
    public class FeedbackModel
    {
        public int? FeedbackID {  get; set; }
        public int? BookingID { get; set; }
        public int? CustID {  get; set; }
        public int? TechID {  get; set; }

        public string TechReview { get; set; }=string.Empty;
        
        public string ServiceReview { get; set; }=string.Empty;
        public string TechRating {  get; set; }=string.Empty;   
        public string ServiceRating {  get; set; }=string.Empty;
    }
}
