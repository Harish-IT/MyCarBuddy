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
                string folderPath = Path.Combine("Images", "PackageImages");
                string rootPath = Path.Combine(_env.WebRootPath, folderPath);

                if (!Directory.Exists(rootPath))
                    Directory.CreateDirectory(rootPath);

                // Save PackageImage
                string packageImagePath = "";
                if (model.PackageImage != null)
                {
                    string fileName = Guid.NewGuid() + Path.GetExtension(model.PackageImage.FileName);
                    string fullPath = Path.Combine(rootPath, fileName);
                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await model.PackageImage.CopyToAsync(stream);
                    }
                    packageImagePath = Path.Combine(folderPath, fileName).Replace("\\", "/");
                }

                // Save multiple BannerImages
                List<string> bannerImagePaths = new List<string>();
                if (model.BannerImages != null && model.BannerImages.Count > 0)
                {
                    foreach (var file in model.BannerImages)
                    {
                        string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                        string fullPath = Path.Combine(rootPath, fileName);
                        using (var stream = new FileStream(fullPath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                        string path = Path.Combine(folderPath, fileName).Replace("\\", "/");
                        bannerImagePaths.Add(path);
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
                    //cmd.Parameters.AddWithValue("@ModifiedBy", model.ModifiedBy);

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


        [HttpPost("update-plan-package")]
        public async Task<IActionResult> UpdatePlanPackage([FromForm] PlanPackageModel model)
        {
            try
            {
                string folderPath = Path.Combine("Images", "PackageImages");
                string rootPath = Path.Combine(_env.WebRootPath, folderPath);

                if (!Directory.Exists(rootPath))
                    Directory.CreateDirectory(rootPath);

                // Handle PackageImage
                string packageImagePath = model.ExistingPackageImage; // fallback to old image
                if (model.PackageImage != null)
                {
                    string fileName = Guid.NewGuid() + Path.GetExtension(model.PackageImage.FileName);
                    string fullPath = Path.Combine(rootPath, fileName);
                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await model.PackageImage.CopyToAsync(stream);
                    }
                    packageImagePath = Path.Combine(folderPath, fileName).Replace("\\", "/");
                }

                // Handle BannerImages
                List<string> bannerImagePaths = new List<string>();
                if (model.BannerImages != null && model.BannerImages.Count > 0)
                {
                    foreach (var file in model.BannerImages)
                    {
                        string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                        string fullPath = Path.Combine(rootPath, fileName);
                        using (var stream = new FileStream(fullPath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                        string path = Path.Combine(folderPath, fileName).Replace("\\", "/");
                        bannerImagePaths.Add(path);
                    }
                }
                else
                {
                    // If no new banner images uploaded, keep old
                    bannerImagePaths = model.ExistingBannerImages?.Split(',').ToList() ?? new List<string>();
                }

                string bannerImageCsv = string.Join(",", bannerImagePaths);

                // Call SP
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

                    return Ok(new { Message = "Package updated successfully" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update failed");
                return StatusCode(500, "Internal error: " + ex.Message);
            }
        }


    }
}
