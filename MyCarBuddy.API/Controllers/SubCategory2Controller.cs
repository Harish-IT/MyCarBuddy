using Braintree;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyCarBuddy.API.Models;
using MyCarBuddy.API.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MyCarBuddy.API.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SubCategory2Controller : ControllerBase
    {
        #region IConfiguration

        private readonly IConfiguration _configuration;
        private readonly ILogger<SubCategory2Controller> _logger;
        private readonly IWebHostEnvironment _env;


        public SubCategory2Controller(IConfiguration configuration, ILogger<SubCategory2Controller> logger, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _logger = logger;
            _env = env;
        }
        #endregion

        #region Insert Category


        private string GetRandomAlphanumericString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        [HttpPost("AddSubCategory2")]
        public async Task<IActionResult> SubCategory2([FromForm] SubCategoriesModel2 subcategory)
        {
            try
            {
                string iconImagePath = string.Empty;
                string thumbnailImagePath = string.Empty;

                // Handle IconImage1 upload
                if (subcategory.IconImage1 != null && subcategory.IconImage1.Length > 0)
                {
                    var iconFolderPath = Path.Combine(_env.WebRootPath, "Images", "SubCategory2");
                    if (!Directory.Exists(iconFolderPath))
                        Directory.CreateDirectory(iconFolderPath);

                    var originalFileName = Path.GetFileNameWithoutExtension(subcategory.IconImage1.FileName);
                    var fileExt = Path.GetExtension(subcategory.IconImage1.FileName);
                    var randomString = GetRandomAlphanumericString(8);
                    string uniqueFileName = $"{originalFileName}_{randomString}{fileExt}";
                    var iconFullPath = Path.Combine(iconFolderPath, uniqueFileName);

                    // Optional: Extra collision check (very rare with random string)
                    int counter = 1;
                    while (System.IO.File.Exists(iconFullPath))
                    {
                        uniqueFileName = $"{originalFileName}_{randomString}_{counter}{fileExt}";
                        iconFullPath = Path.Combine(iconFolderPath, uniqueFileName);
                        counter++;
                    }

                    using (var stream = new FileStream(iconFullPath, FileMode.Create))
                    {
                        await subcategory.IconImage1.CopyToAsync(stream);
                    }

                    iconImagePath = Path.Combine("SubCategory2", uniqueFileName).Replace("\\", "/");
                }

                // Handle ThumbnailImage1 upload
                if (subcategory.ThumbnailImage1 != null && subcategory.ThumbnailImage1.Length > 0)
                {
                    var thumbFolderPath = Path.Combine(_env.WebRootPath, "Images", "SubCategory2");
                    if (!Directory.Exists(thumbFolderPath))
                        Directory.CreateDirectory(thumbFolderPath);

                    var originalFileName = Path.GetFileNameWithoutExtension(subcategory.ThumbnailImage1.FileName);
                    var fileExt = Path.GetExtension(subcategory.ThumbnailImage1.FileName);
                    var randomString = GetRandomAlphanumericString(8);
                    string uniqueFileName = $"{originalFileName}_{randomString}{fileExt}";
                    var thumbFullPath = Path.Combine(thumbFolderPath, uniqueFileName);

                    // Optional: Extra collision check (very rare with random string)
                    int counter = 1;
                    while (System.IO.File.Exists(thumbFullPath))
                    {
                        uniqueFileName = $"{originalFileName}_{randomString}_{counter}{fileExt}";
                        thumbFullPath = Path.Combine(thumbFolderPath, uniqueFileName);
                        counter++;
                    }

                    using (var stream = new FileStream(thumbFullPath, FileMode.Create))
                    {
                        await subcategory.ThumbnailImage1.CopyToAsync(stream);
                    }

                    thumbnailImagePath = Path.Combine("SubCategory2", uniqueFileName).Replace("\\", "/");
                }

                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_InsertServiceSubCategory2", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@SubCategoryID", subcategory.SubCategoryID);
                        cmd.Parameters.AddWithValue("@Name", subcategory.Name);
                        cmd.Parameters.AddWithValue("@Description", subcategory.Description ?? "");
                        cmd.Parameters.AddWithValue("@IsActive", subcategory.IsActive);
                        cmd.Parameters.AddWithValue("@IconImage", iconImagePath);
                        cmd.Parameters.AddWithValue("@ThumbnailImage", thumbnailImagePath);
                        cmd.Parameters.AddWithValue("@CreatedBy", subcategory.CreatedBy ?? 0);

                        conn.Open();
                        int rows = cmd.ExecuteNonQuery();

                        if (rows > 0)
                        {
                            return Ok(new { status = true, message = " SubCategory-2 inserted successfully." });
                        }
                        else
                        {
                            return BadRequest(new { status = false, message = "SubCategory-2 not inserted." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { status = false, message = "An error occurred while inserting the record.", error = ex.Message });
            }
        }

        #endregion


        #region Update sub category 2


        [HttpPut("UpdateSubCategory2")]
        public async Task<IActionResult> UpdateSubCategory2([FromForm] SubCategoriesModel2 subcategory2)
        {
            try
            {
                string iconImagePath = subcategory2.IconImage ?? string.Empty;
                string thumbnailImagePath = subcategory2.ThumbnailImage ?? string.Empty;

                // Retrieve from DB if missing and no new file uploaded
                if ((string.IsNullOrEmpty(iconImagePath) && (subcategory2.IconImage1 == null || subcategory2.IconImage1.Length == 0)) ||
                    (string.IsNullOrEmpty(thumbnailImagePath) && (subcategory2.ThumbnailImage1 == null || subcategory2.ThumbnailImage1.Length == 0)))
                {
                    using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                    using (SqlCommand cmd = new SqlCommand("sp_GetSubCategory2ImagesByID", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@SubSubCategoryID", subcategory2.SubSubCategoryID);
                        await conn.OpenAsync();
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                if (string.IsNullOrEmpty(iconImagePath))
                                    iconImagePath = reader["IconImage"]?.ToString();
                                if (string.IsNullOrEmpty(thumbnailImagePath))
                                    thumbnailImagePath = reader["ThumbnailImage"]?.ToString();
                            }
                        }
                    }
                }

                // Handle IconImage1 upload
                if (subcategory2.IconImage1 != null && subcategory2.IconImage1.Length > 0)
                {
                    var iconFolderPath = Path.Combine(_env.WebRootPath, "Images", "SubCategory2");
                    if (!Directory.Exists(iconFolderPath))
                        Directory.CreateDirectory(iconFolderPath);

                    var originalFileName = Path.GetFileNameWithoutExtension(subcategory2.IconImage1.FileName);
                    var fileExt = Path.GetExtension(subcategory2.IconImage1.FileName);
                    var randomString = GetRandomAlphanumericString(8);
                    string uniqueFileName = $"{originalFileName}_{randomString}{fileExt}";
                    var iconFullPath = Path.Combine(iconFolderPath, uniqueFileName);

                    // Optional: Extra collision check (very rare with random string)
                    int counter = 1;
                    while (System.IO.File.Exists(iconFullPath))
                    {
                        uniqueFileName = $"{originalFileName}_{randomString}_{counter}{fileExt}";
                        iconFullPath = Path.Combine(iconFolderPath, uniqueFileName);
                        counter++;
                    }

                    using (var stream = new FileStream(iconFullPath, FileMode.Create))
                    {
                        await subcategory2.IconImage1.CopyToAsync(stream);
                    }

                    iconImagePath = Path.Combine("SubCategory2", uniqueFileName).Replace("\\", "/");
                }

                // Handle ThumbnailImage1 upload
                if (subcategory2.ThumbnailImage1 != null && subcategory2.ThumbnailImage1.Length > 0)
                {
                    var thumbFolderPath = Path.Combine(_env.WebRootPath, "Images", "SubCategory2");
                    if (!Directory.Exists(thumbFolderPath))
                        Directory.CreateDirectory(thumbFolderPath);

                    var originalFileName = Path.GetFileNameWithoutExtension(subcategory2.ThumbnailImage1.FileName);
                    var fileExt = Path.GetExtension(subcategory2.ThumbnailImage1.FileName);
                    var randomString = GetRandomAlphanumericString(8);
                    string uniqueFileName = $"{originalFileName}_{randomString}{fileExt}";
                    var thumbFullPath = Path.Combine(thumbFolderPath, uniqueFileName);

                    // Optional: Extra collision check (very rare with random string)
                    int counter = 1;
                    while (System.IO.File.Exists(thumbFullPath))
                    {
                        uniqueFileName = $"{originalFileName}_{randomString}_{counter}{fileExt}";
                        thumbFullPath = Path.Combine(thumbFolderPath, uniqueFileName);
                        counter++;
                    }

                    using (var stream = new FileStream(thumbFullPath, FileMode.Create))
                    {
                        await subcategory2.ThumbnailImage1.CopyToAsync(stream);
                    }

                    thumbnailImagePath = Path.Combine("SubCategory2", uniqueFileName).Replace("\\", "/");
                }

                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_UpdateServiceSubCategories2", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@SubSubCategoryID", subcategory2.SubSubCategoryID);
                        cmd.Parameters.AddWithValue("@SubCategoryID", subcategory2.SubCategoryID);
                        cmd.Parameters.AddWithValue("@Name", subcategory2.Name);
                        cmd.Parameters.AddWithValue("@Description", subcategory2.Description);
                        cmd.Parameters.AddWithValue("@IsActive", subcategory2.IsActive);


                        cmd.Parameters.AddWithValue("@IconImage", iconImagePath);
                        cmd.Parameters.AddWithValue("@ThumbnailImage", thumbnailImagePath);
                        cmd.Parameters.AddWithValue("@ModifiedBy", subcategory2.ModifiedBy);
                        conn.Open();
                        int rows = cmd.ExecuteNonQuery();

                        if (rows > 0)
                        {
                            return Ok(new { status = true, message = " Sub Category-2 updated successfully." });
                        }
                        else
                        {
                            return BadRequest(new { status = false, message = "Sub Category-2 not updated." });
                        }
                    }
                }


            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { status = false, message = "An error occurred while inserting the record.", error = ex.Message });
            }
        }

        #endregion

        #region GetListSubCategory2

        [HttpGet]

        public IActionResult GetListSubCategory2()
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetListServiceSubCategories2", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            dt.Load(reader);
                        }
                        conn.Close();
                    }
                    var Data = new List<Dictionary<string, object>>();
                    foreach (DataRow row in dt.Rows)
                    {
                        var dict = new Dictionary<string, object>();
                        foreach (DataColumn col in dt.Columns)
                        {
                            dict[col.ColumnName] = row[col];
                        }
                        Data.Add(dict);
                    }
                    return Ok(new { status = true, Data });
                }
            }
            catch (Exception ex)
            {

                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while retrieving the categories.", error = ex.Message });

            }
        }

        #endregion

        #region SubCategory1ById


        [HttpGet("subcategoryid2")]

        public IActionResult GetSubCategory1ById(int subcategoryid2)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetListServiceSubCategories2ByID", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@SubSubCategoryID", subcategoryid2);
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            dt.Load(reader);
                        }
                        conn.Close();
                    }
                }
                if (dt.Rows.Count == 0)
                {
                    return NotFound(new { message = "Sub Category-2 not found.." });
                }
                var Data = new List<Dictionary<string, object>>();
                foreach (DataRow row in dt.Rows)
                {
                    var dict = new Dictionary<string, object>();
                    foreach (DataColumn col in dt.Columns)
                    {
                        dict[col.ColumnName] = row[col];
                    }
                    Data.Add(dict);
                }
                return Ok(Data.Count == 1 ? Data[0] : Data);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while retrieving the Sub Categories-2.", error = ex.Message });

            }

        }

        #endregion

        #region DeleteCategory

        [HttpDelete("subcategoryid2")]

        public IActionResult DeleteSubCategory1(int subcategoryid2)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_DeleteServiceSubCategories2ByID", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@SubSubCategoryID", subcategoryid2);
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int resultCode = Convert.ToInt32(reader["ResultCode"]);
                                string message = reader["Message"].ToString();

                                if (resultCode == 1)
                                    return Ok(new { message });
                                else if (resultCode == -1)
                                    return BadRequest(new { message });
                                else
                                    return NotFound(new { message });
                            }
                        }
                        conn.Close();
                    }
                }
                return StatusCode(500, new { message = "Unknown error occurred." });
            }
            catch (Exception ex)
            {

                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while deleting the record.", error = ex.Message });

            }
        }

        #endregion

    }
}
