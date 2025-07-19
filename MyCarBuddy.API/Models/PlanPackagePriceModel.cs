using Microsoft.AspNetCore.Http;

namespace MyCarBuddy.API.Models
{
    public class PlanPackagePriceModel
    {
        public int?PlanPriceID { get; set; }
        public int? BrandID { get; set; }
        public int? ModelID {  get; set; }
        public int? FuelTypeID { get; set; }
        public int? PackageID {  get; set; }
        public string Description {  get; set; }
        public int? EstimatedDurationMinutes { get; set; }
        public IFormFile ImageURL1 { get; set; } // For uploading the image
        public string ImageURL { get; set; } = string.Empty; // For saving the image path to DB
        public bool? IsActive {  get; set; }
        public decimal Serv_Reg_Price {  get; set; }
        public decimal Serv_Off_Price {  get; set; }
        public int? CreatedBy {  get; set; }

        public int? ModifiedBy { get; set; }



    }
}
