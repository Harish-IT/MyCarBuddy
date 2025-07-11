using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;

namespace MyCarBuddy.API.Models
{
    public class VehicleBrandsModel
    {
      
        public int? BrandID { get; set; }

        public string BrandName {  get; set; }
        public string BrandLogo {  get; set; }
        public IFormFile BrandLogoImage { get; set; }

      
        public bool? IsActive {  get; set; }

        public int? CreatedBy {  get; set; }

        public int? ModifiedBy { get; set; }

        public int? Status { get; set; }


    }
}
