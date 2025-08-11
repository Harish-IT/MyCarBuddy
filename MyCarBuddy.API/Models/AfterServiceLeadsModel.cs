namespace MyCarBuddy.API.Models
{
    public class AfterServiceLeadsModel
    {
        public int? ID { get;set; }
        public string Reason { get; set; } = string.Empty;

    }

    public class ServiceLeads
    {
        public int? ID { get;set; }
        public string BookingID {  get; set; }
        public int? PackageID { get; set; }
        public int? IncludeID { get; set;}
        public string IncludeName {  get; set; }
        public bool Status { get; set; }
        public string Reasons { get; set; }
        public int? TechID { get; set; }

    }
}
