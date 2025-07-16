using Microsoft.AspNetCore.Http;

namespace MyCarBuddy.API.Models
{
    public class VehicleModelsClass
    {

        public int? ModelID {  get; set; }
        public int? BrandID {  get; set; }
        public string ModelName { get; set; }

        public int? FuelTypeID {  get; set; }

        public string VehicleImage {  get; set; }
        public IFormFile VehicleImages1 { get; set; }

        public bool? IsActive { get; set; }
        public int? CreatedBy { get; set; }
        public int? ModifiedBy {  get; set; }
        public int? Status {  get; set; }

    






    }
}
