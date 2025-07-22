using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Collections.Generic;

namespace MyCarBuddy.API.Models
{
    public class PlanPackageModel
    {
        public int PackageID { get; set; }
        public string PackageName { get; set; }
        public int CategoryID { get; set; }
        public int SubCategoryID { get; set; }
        public string IncludeID { get; set; } // comma-separated values
        public IFormFile PackageImage { get; set; }
        public List<IFormFile> BannerImages { get; set; } // for multiple
        public decimal TotalPrice { get; set; }
        public int Status { get; set; }
        public int CreatedBy { get; set; }
        public int ModifiedBy { get; set; }

        public string ExistingPackageImage { get; set; }
        public string ExistingBannerImages { get; set; }
    }

    public class PlanPackageDTO
    {
        public int PackageID { get; set; }
        public string PackageName { get; set; }
        public int CategoryID { get; set; }
        public string CategoryName { get; set; }
        public int SubCategoryID { get; set; }
        public string SubCategoryName { get; set; }
        public string IncludeID { get; set; }
        public string IncludeNames { get; set; }
        public string IncludePrices { get; set; }
        public string PackageImage { get; set; }
        public string BannerImage { get; set; }
        public bool IsActive { get; set; }
        public decimal TotalPrice { get; set; }

        public decimal Serv_Off_Price { get; set; }

        public decimal Serv_Reg_Price { get; set; }
    }

}
