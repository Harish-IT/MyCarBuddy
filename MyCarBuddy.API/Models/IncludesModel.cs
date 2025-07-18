namespace MyCarBuddy.API.Models
{
    public class IncludesModel
    {
        public int? IncludeId {  get; set; }
        
        public int? SubCategoryID { get; set; }
        public string IncludeName { get; set; }
        public string Description {  get; set; }
        public decimal IncludePrice {  get; set; }
        public int? CreatedBy {  get; set; }
        public int? ModifiedBy { get; set; }
        public bool? IsActive { get; set; }

        public int? CategoryID { get; set; }

    }
}
