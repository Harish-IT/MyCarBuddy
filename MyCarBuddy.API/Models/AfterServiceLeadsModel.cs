using System.Collections.Generic;

namespace MyCarBuddy.API.Models
{
    public class AfterServiceLeadsModel
    {
        public int? ID { get;set; }
        public string Reason { get; set; } = string.Empty;
        public bool IsActive {  get; set; }
        public string ReasonType { get; set; }=string.Empty;

    }

    public class ServiceLeadRequest
    {
        public string BookingID { get; set; }
        public string Reasons { get; set; }
        public int? TechID { get; set; }
        public List<PackageModel> Packages { get; set; }
    }

    public class PackageModel
    {
        public int PackageID { get; set; }
        public List<IncludeModel> Includes { get; set; }
    }

    public class IncludeModel
    {
        public int IncludeID { get; set; }
        public string IncludeName { get; set; }
        public bool Status { get; set; }
    }

}
