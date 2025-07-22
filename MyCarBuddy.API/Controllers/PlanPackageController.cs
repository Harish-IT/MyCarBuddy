using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyCarBuddy.API.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MyCarBuddy.API.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PlanPackageController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<CategoryController> _logger;
        private readonly IWebHostEnvironment _env;


        public PlanPackageController(IConfiguration configuration, ILogger<CategoryController> logger, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _logger = logger;
            _env = env;
        }

        [HttpPost("InsertPlanPackage")]
        public async Task<IActionResult> InsertPlanPackage([FromForm] PlanPackageModel model)
        {
            try
            {
                string folderPath = Path.Combine("Images","PackageImages");
                string rootPath = Path.Combine(_env.WebRootPath, folderPath);

                if (!Directory.Exists(rootPath))
                    Directory.CreateDirectory(rootPath);

                // Get existing image count for prefixing
                int existingFileCount = Directory.GetFiles(rootPath).Length;
                int imageIndex = existingFileCount + 1;

                // Save PackageImage
                string packageImagePath = "";
                if (model.PackageImage != null)
                {
                    string originalName = Path.GetFileName(model.PackageImage.FileName);
                    string prefix = imageIndex.ToString("D3"); // e.g., 001
                    string fileName = $"{prefix}{originalName}";
                    string fullPath = Path.Combine(rootPath, fileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await model.PackageImage.CopyToAsync(stream);
                    }

                    //packageImagePath = Path.Combine(folderPath, fileName).Replace("\\", "/");
                    packageImagePath = Path.Combine("PackageImages", fileName).Replace("\\", "/");
                    imageIndex++;
                }

                // Save multiple BannerImages
                List<string> bannerImagePaths = new List<string>();
                if (model.BannerImages != null && model.BannerImages.Count > 0)
                {
                    foreach (var file in model.BannerImages)
                    {
                        string originalName = Path.GetFileName(file.FileName);
                        string prefix = imageIndex.ToString("D3");
                        string fileName = $"{prefix}{originalName}";
                        string fullPath = Path.Combine(rootPath, fileName);

                        using (var stream = new FileStream(fullPath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        //string path = Path.Combine(folderPath, fileName).Replace("\\", "/");
                        string path = Path.Combine("PackageImages", fileName).Replace("\\", "/");
                        bannerImagePaths.Add(path);
                        imageIndex++;
                    }
                }

                string bannerImageCsv = string.Join(",", bannerImagePaths);

                // Insert into DB using SP
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    SqlCommand cmd = new SqlCommand("SP_InsertPlanPackage", conn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@PackageName", model.PackageName);
                    cmd.Parameters.AddWithValue("@CategoryID", model.CategoryID);
                    cmd.Parameters.AddWithValue("@SubCategoryID", model.SubCategoryID);
                    cmd.Parameters.AddWithValue("@IncludeID", model.IncludeID ?? "");
                    cmd.Parameters.AddWithValue("@PackageImage", packageImagePath);
                    cmd.Parameters.AddWithValue("@BannerImage", bannerImageCsv);
                    cmd.Parameters.AddWithValue("@TotalPrice", model.TotalPrice);
                    cmd.Parameters.AddWithValue("@Status", model.Status);
                    cmd.Parameters.AddWithValue("@CreatedBy", model.CreatedBy);

                    conn.Open();
                    var newId = cmd.ExecuteScalar();
                    conn.Close();

                    return Ok(new { Message = "Package inserted successfully", PackageID = newId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InsertWithImages failed");
                return StatusCode(500, "Internal error: " + ex.Message);
            }
        }



        [HttpGet("GetPlanPackagesDetails")]
        public IActionResult GetPlanPackagesDetails()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            List<PlanPackageDTO> packages = new List<PlanPackageDTO>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand("SP_GetPlanPackagesDetails", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    packages.Add(new PlanPackageDTO
                    {
                        PackageID = Convert.ToInt32(reader["PackageID"]),
                        PackageName = reader["PackageName"].ToString(),
                        CategoryID = Convert.ToInt32(reader["CategoryID"]),
                        CategoryName = reader["CategoryName"].ToString(),
                        SubCategoryID = Convert.ToInt32(reader["SubCategoryID"]),
                        SubCategoryName = reader["SubCategoryName"].ToString(),
                        IncludeID = reader["IncludeID"].ToString(),
                        IncludeNames = reader["IncludeNames"].ToString(),
                        IncludePrices = reader["IncludePrices"].ToString(),
                        PackageImage = reader["PackageImage"].ToString(),
                        BannerImage = reader["BannerImage"].ToString(),
                        IsActive = Convert.ToBoolean(reader["IsActive"])
                    });
                }
                conn.Close();
            }

            return Ok(packages);
        }


        [HttpPost("UpdatePlanPackage")]
        public async Task<IActionResult> UpdatePlanPackage([FromForm] PlanPackageModel model)
        {
            try
            {
                string relativeFolderPath = Path.Combine("Images", "PackageImages");
                string fullFolderPath = Path.Combine(_env.WebRootPath, relativeFolderPath);

                if (!Directory.Exists(fullFolderPath))
                    Directory.CreateDirectory(fullFolderPath);

                // ---- Package Image ----
                string packageImagePath = model.ExistingPackageImage ?? "";

                if (model.PackageImage != null && model.PackageImage.Length > 0)
                {
                    // Delete old file if it exists
                    if (!string.IsNullOrEmpty(model.ExistingPackageImage))
                    {
                        string oldPath = Path.Combine(_env.WebRootPath, model.ExistingPackageImage.Replace("/", "\\"));
                        if (System.IO.File.Exists(oldPath))
                            System.IO.File.Delete(oldPath);
                    }

                    // Save new file
                    string fileName = Guid.NewGuid() + Path.GetExtension(model.PackageImage.FileName);
                    string newPath = Path.Combine(fullFolderPath, fileName);
                    using (var stream = new FileStream(newPath, FileMode.Create))
                    {
                        await model.PackageImage.CopyToAsync(stream);
                    }

                    packageImagePath = Path.Combine("PackageImages", fileName).Replace("\\", "/");
                }

                // ---- Banner Images ----
                List<string> bannerImagePaths = new List<string>();

                if (model.BannerImages != null && model.BannerImages.Count > 0)
                {
                    // Delete old banner images
                    if (!string.IsNullOrEmpty(model.ExistingBannerImages))
                    {
                        foreach (var existing in model.ExistingBannerImages.Split(','))
                        {
                            string oldPath = Path.Combine(_env.WebRootPath, existing.Replace("/", "\\"));
                            if (System.IO.File.Exists(oldPath))
                                System.IO.File.Delete(oldPath);
                        }
                    }

                    // Save new banner images
                    foreach (var file in model.BannerImages)
                    {
                        string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                        string newPath = Path.Combine(fullFolderPath, fileName);
                        using (var stream = new FileStream(newPath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                        //string path = Path.Combine(relativeFolderPath, fileName).Replace("\\", "/");
                        string path = Path.Combine("PackageImages", fileName).Replace("\\", "/");
                        bannerImagePaths.Add(path);
                    }
                }
                else
                {
                    // No new images — keep existing ones
                    if (!string.IsNullOrEmpty(model.ExistingBannerImages))
                        bannerImagePaths = model.ExistingBannerImages.Split(',').ToList();
                }

                string bannerImageCsv = string.Join(",", bannerImagePaths);

                // ---- Call Stored Procedure ----
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    SqlCommand cmd = new SqlCommand("SP_UpdatePlanPackage", conn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@PackageID", model.PackageID);
                    cmd.Parameters.AddWithValue("@PackageName", model.PackageName);
                    cmd.Parameters.AddWithValue("@CategoryID", model.CategoryID);
                    cmd.Parameters.AddWithValue("@SubCategoryID", model.SubCategoryID);
                    cmd.Parameters.AddWithValue("@IncludeID", model.IncludeID ?? "");
                    cmd.Parameters.AddWithValue("@PackageImage", packageImagePath);
                    cmd.Parameters.AddWithValue("@BannerImage", bannerImageCsv);
                    cmd.Parameters.AddWithValue("@TotalPrice", model.TotalPrice);
                    cmd.Parameters.AddWithValue("@Status", model.Status);
                    cmd.Parameters.AddWithValue("@ModifiedBy", model.ModifiedBy);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }

                return Ok(new { Message = "Package updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update failed");
                return StatusCode(500, "Internal error: " + ex.Message);
            }
        }




        [HttpGet("GetPlanPackageDetailsByID/{id}")]
        public IActionResult GetPlanPackageDetailsByID(int id)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            PlanPackageDTO package = null;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand("SP_GetPlanPackageDetailsByID", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@PackageID", id);

                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    package = new PlanPackageDTO
                    {
                        PackageID = Convert.ToInt32(reader["PackageID"]),
                        PackageName = reader["PackageName"].ToString(),
                        CategoryID = Convert.ToInt32(reader["CategoryID"]),
                        CategoryName = reader["CategoryName"].ToString(),
                        SubCategoryID = Convert.ToInt32(reader["SubCategoryID"]),
                        SubCategoryName = reader["SubCategoryName"].ToString(),
                        IncludeID = reader["IncludeID"].ToString(),
                        IncludeNames = reader["IncludeNames"].ToString(),
                        IncludePrices = reader["IncludePrices"].ToString(),
                        PackageImage = reader["PackageImage"].ToString(),
                        BannerImage = reader["BannerImage"].ToString(),
                        IsActive = Convert.ToBoolean(reader["IsActive"]),
                        TotalPrice = Convert.ToDecimal(reader["TotalPrice"])
                    };
                }
                conn.Close();
            }

            if (package == null)
                return NotFound();

            return Ok(package);
        }


        [HttpGet("GetPlanPackagesByCategoryAndSubCategory")]
        public IActionResult GetPlanPackagesByCategoryAndSubCategory(
      [FromQuery] int? categoryId,
      [FromQuery] int? subCategoryId,
      [FromQuery] int? BrandID,
      [FromQuery] int? ModelID,
      [FromQuery] int? FuelTypeID)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            List<PlanPackageDTO> packages = new List<PlanPackageDTO>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SP_GetPlanPackageDetailsByCateSubCateID", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Null-safe parameters
                    cmd.Parameters.AddWithValue("@CategoryID", categoryId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@SubCategoryID", subCategoryId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@BrandID", BrandID ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ModelID", ModelID ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@FuelTypeID", FuelTypeID ?? (object)DBNull.Value);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            packages.Add(new PlanPackageDTO
                            {
                                PackageID = reader["PackageID"] != DBNull.Value ? Convert.ToInt32(reader["PackageID"]) : 0,
                                PackageName = reader["PackageName"]?.ToString(),
                                CategoryID = reader["CategoryID"] != DBNull.Value ? Convert.ToInt32(reader["CategoryID"]) : 0,
                                CategoryName = reader["CategoryName"]?.ToString(),
                                SubCategoryID = reader["SubCategoryID"] != DBNull.Value ? Convert.ToInt32(reader["SubCategoryID"]) : 0,
                                SubCategoryName = reader["SubCategoryName"]?.ToString(),
                                IncludeID = reader["IncludeID"]?.ToString(),
                                IncludeNames = reader["IncludeNames"]?.ToString(),
                                IncludePrices = reader["IncludePrices"]?.ToString(),
                                PackageImage = reader["PackageImage"]?.ToString(),
                                BannerImage = reader["BannerImage"]?.ToString(),
                                IsActive = reader["IsActive"] != DBNull.Value && Convert.ToBoolean(reader["IsActive"]),
                                Serv_Off_Price = reader["Serv_Off_Price"] != DBNull.Value ? Convert.ToDecimal(reader["Serv_Off_Price"]) : 0,
                                Serv_Reg_Price = reader["Serv_Reg_Price"] != DBNull.Value ? Convert.ToDecimal(reader["Serv_Reg_Price"]) : 0
                            });
                        }
                    }
                }
            }

            if (packages.Count == 0)
                return NotFound();

            return Ok(packages);
        }


    }
}
