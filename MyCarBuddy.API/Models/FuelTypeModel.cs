using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MyCarBuddy.API.Models
{
    public class FuelTypeModel
    {
        public int? FuelTypeID {  get; set; }

        public string FuelTypeName {  get; set; }
         public string FuelImage {  get; set; }
        public  IFormFile FuelImage1 { get; set; }
        public bool ?IsActive { get; set; }
        public int? CreatedBy { get; set; }
        public int? ModifiedBy {  get; set; }

        public int? Status { get; set; }



    }
}
