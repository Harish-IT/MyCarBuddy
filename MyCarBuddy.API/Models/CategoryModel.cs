using System;

namespace MyCarBuddy.API.Models
{
    public class CategoryModel
    {
        public int? CategoryID { get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; }

        public bool? IsActive { get; set; }
        public int? CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }




    }
}
