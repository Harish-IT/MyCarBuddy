using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyCarBuddy.API.Models;
using MyCarBuddy.API.Utilities;

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
        #region IConfiguration
        private readonly IConfiguration _configuration;
        private readonly ILogger<CategoryController> _logger;
        private readonly IWebHostEnvironment _env;


        public PlanPackageController(IConfiguration configuration, ILogger<CategoryController> logger, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _logger = logger;
            _env = env;
        }

        #endregion

        #region InsertPlanPackage

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
                    cmd.Parameters.AddWithValue("@Description",model.Description??(object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@EstimatedDurationMinutes",model.EstimatedDurationMinutes ?? (object)DBNull.Value);

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

        #endregion

        #region GetPlanPackagesDetails

        [HttpGet("GetPlanPackagesDetails")]
        public IActionResult GetPlanPackagesDetails()
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("SP_GetPlanPackagesDetails", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            dt.Load(reader); // Load entire result into DataTable
                        }
                        conn.Close();
                    }
                }

                if (dt.Rows.Count == 0)
                {
                    return NotFound(new { message = "Plan Packages not found" });
                }
                var Data = new List<Dictionary<string, object>>();
                foreach (DataRow row in dt.Rows)
                {
                    var dict = new Dictionary<string, object>();
                    foreach (DataColumn col in dt.Columns)
                    {
                        var value = row[col];
                        dict[col.ColumnName] = value == DBNull.Value ? null : value;
                    }
                    Data.Add(dict);
                }

                return Ok(Data.Count == 1 ? Data[0] : Data);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while retrieving the plan packages.", error = ex.Message });
            }
        }

        #endregion

        #region UpdatePlanPackage

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

                    cmd.Parameters.AddWithValue("@Description",
                        string.IsNullOrWhiteSpace(model.Description) ? DBNull.Value : model.Description);

                    cmd.Parameters.AddWithValue("@EstimatedDurationMinutes",
                        model.EstimatedDurationMinutes.HasValue ? model.EstimatedDurationMinutes.Value : DBNull.Value);
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

        #endregion

        #region GetPlanPackageDetailsByID

        [HttpGet("GetPlanPackageDetailsByID/{id}")]
        public IActionResult GetPlanPackageDetailsByID(int id)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("SP_GetPlanPackageDetailsByID", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@PackageID", id);
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            dt.Load(reader); // Load the result into DataTable
                        }
                        conn.Close();
                    }
                }

                if (dt.Rows.Count == 0)
                {
                    return NotFound(new { message = "Plan Packages not found" });
                }
                var Data = new List<Dictionary<string, object>>();
                foreach (DataRow row in dt.Rows)
                {
                    var dict = new Dictionary<string, object>();
                    foreach (DataColumn col in dt.Columns)
                    {
                        var value = row[col];
                        dict[col.ColumnName] = value == DBNull.Value ? null : value;
                    }
                    Data.Add(dict);
                }

                return Ok(Data.Count == 1 ? Data[0] : Data);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while retrieving the plan package.", error = ex.Message });
            }
        }

        #endregion

        #region GetPlanPackagesByCategoryAndSubCategory

        [HttpGet("GetPlanPackagesByCategoryAndSubCategory")]
        public IActionResult GetPlanPackagesByCategoryAndSubCategory(
            [FromQuery] int? categoryId,
            [FromQuery] int? subCategoryId,
            [FromQuery] int? BrandID,
            [FromQuery] int? ModelID,
            [FromQuery] int? FuelTypeID)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("SP_GetPlanPackageDetailsByCateSubCateID", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@CategoryID", categoryId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@SubCategoryID", subCategoryId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@BrandID", BrandID ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ModelID", ModelID ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@FuelTypeID", FuelTypeID ?? (object)DBNull.Value);

                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            dt.Load(reader); // Load result into DataTable
                        }
                        conn.Close();
                    }
                }


                if (dt.Rows.Count == 0)
                {
                    return NotFound(new { message = "Plan Packages By Category And SubCategory not found" });
                }
                var Data = new List<Dictionary<string, object>>();
                foreach (DataRow row in dt.Rows)
                {
                    var dict = new Dictionary<string, object>();
                    foreach (DataColumn col in dt.Columns)
                    {
                        var value = row[col];
                        dict[col.ColumnName] = value == DBNull.Value ? null : value;
                    }
                    Data.Add(dict);
                }

                return Ok(Data.Count == 1 ? Data[0] : Data);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while retrieving packages.", error = ex.Message });
            }
        }

        #endregion
    }
}
