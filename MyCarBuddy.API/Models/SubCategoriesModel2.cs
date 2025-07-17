using Azure;
using Microsoft.AspNetCore.Http;

namespace MyCarBuddy.API.Models
{
    public class SubCategoriesModel2
    {
        public int? SubSubCategoryID { get; set; }

        public int SubCategoryID {  get; set; }

        public string Name {  get; set; }

        public string Description { get; set; }
       
        public IFormFile IconImage1 { get; set; }
        public string IconImage { get; set; } = string.Empty;

        public IFormFile ThumbnailImage1 {  get; set; }
        public string ThumbnailImage { get; set; }=string.Empty;

        public int? CreatedBy { get; set; }

        public int? ModifiedBy { get; set; }

    }
}
