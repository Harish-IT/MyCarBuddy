using Microsoft.AspNetCore.Http;
using System;

namespace MyCarBuddy.API.Models
{
    public class CategoryModel
    {
        // insert model class
        public int? CategoryID { get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; }

        public bool? IsActive { get; set; }
        public int? CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }

        public IFormFile IconImage1 { get; set; }

        public string IconImage { get; set; } = string.Empty;

        public IFormFile ThumbnailImage1 { get; set; }

        public string ThumbnailImage { get; set; } = string.Empty;



    }

    // update mdoel class

    public class Updatecategory
    {
        public int CategoryID {  get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; }

        public bool? IsActive { get; set; }
        public int? ModifiedBy { get; set; }
        public IFormFile IconImage1 { get; set; }

        public string IconImage { get; set; } = string.Empty;

        public IFormFile ThumbnailImage1 { get; set; }

        public string ThumbnailImage { get; set; } = string.Empty;



    }
}
