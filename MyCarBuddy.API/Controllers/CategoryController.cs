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
    public class CategoryController : ControllerBase
    {

        private readonly IConfiguration _configuration;
        private readonly ILogger<CategoryController> _logger;
        private readonly IWebHostEnvironment _env;


        public CategoryController(IConfiguration configuration, ILogger<CategoryController> logger, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _logger = logger;
            _env = env;
        }

        #region Insert Category


        private string GetRandomAlphanumericString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        [HttpPost("AddCategory")]
        public async Task<IActionResult> Category([FromForm] CategoryModel category)
        {
            try
            {
                string iconImagePath = string.Empty;
                string thumbnailImagePath = string.Empty;

                if (category.IconImage1 != null && category.IconImage1.Length > 0)
                {
                    var iconFolderPath = Path.Combine(_env.WebRootPath, "Images", "Category");
                    if (!Directory.Exists(iconFolderPath))
                        Directory.CreateDirectory(iconFolderPath);

                    //var iconFileName = Path.GetFileName(category.IconImage1.FileName);
                    //var iconFullPath = Path.Combine(iconFolderPath, iconFileName);


                    var originalIconFileName = Path.GetFileNameWithoutExtension(category.IconImage1.FileName);
                    var iconFileExt = Path.GetExtension(category.IconImage1.FileName);
                    var randomString = GetRandomAlphanumericString(8); // 8-character alphanumeric
                    var iconFileName = $"{originalIconFileName}_{randomString}{iconFileExt}";
                    var iconFullPath = Path.Combine(iconFolderPath, iconFileName);


                    int counter = 1;
                    while (System.IO.File.Exists(iconFullPath))
                    {
                        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(iconFileName);
                        var fileExt = Path.GetExtension(iconFileName);
                        iconFileName = $"{fileNameWithoutExt}_{counter}{fileExt}";
                        iconFullPath = Path.Combine(iconFolderPath, iconFileName);
                        counter++;
                    }

                    using (var stream = new FileStream(iconFullPath, FileMode.Create))
                    {
                        await category.IconImage1.CopyToAsync(stream);
                    }

                    iconImagePath = Path.Combine("Category", iconFileName).Replace("\\", "/");
                }

                if (category.ThumbnailImage1 != null && category.ThumbnailImage1.Length > 0)
                {
                    var thumbFolderPath = Path.Combine(_env.WebRootPath, "Images", "Category");
                    if (!Directory.Exists(thumbFolderPath))
                        Directory.CreateDirectory(thumbFolderPath);

                    //var thumbFileName = Path.GetFileName(category.ThumbnailImage1.FileName);
                    //var thumbFullPath = Path.Combine(thumbFolderPath, thumbFileName);


                    var originalThumbFileName = Path.GetFileNameWithoutExtension(category.ThumbnailImage1.FileName);
                    var thumbFileExt = Path.GetExtension(category.ThumbnailImage1.FileName);
                    var randomString = GetRandomAlphanumericString(8); // 8-character alphanumeric
                    var thumbFileName = $"{originalThumbFileName}_{randomString}{thumbFileExt}";
                    var thumbFullPath = Path.Combine(thumbFolderPath, thumbFileName);

                    int counter = 1;
                    while (System.IO.File.Exists(thumbFullPath))
                    {
                        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(thumbFileName);
                        var fileExt = Path.GetExtension(thumbFileName);
                        thumbFileName = $"{fileNameWithoutExt}_{counter}{fileExt}";
                        thumbFullPath = Path.Combine(thumbFolderPath, thumbFileName);
                        counter++;
                    }

                    using (var stream = new FileStream(thumbFullPath, FileMode.Create))
                    {
                        await category.ThumbnailImage1.CopyToAsync(stream);
                    }

                    thumbnailImagePath = Path.Combine("Category", thumbFileName).Replace("\\", "/");
                }


                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_InsertServiceCategory", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@CategoryName", category.CategoryName ?? "");
                        cmd.Parameters.AddWithValue("@Description", category.Description ?? "");
                        cmd.Parameters.AddWithValue("@IsActive", category.IsActive);
                        cmd.Parameters.AddWithValue("@IconImage", iconImagePath);
                        cmd.Parameters.AddWithValue("@ThumbnailImage", thumbnailImagePath);
                        cmd.Parameters.AddWithValue("@CreatedBy", category.CreatedBy ?? 0);

                        conn.Open();
                        int rows = cmd.ExecuteNonQuery();

                        if (rows > 0)
                        {
                            return Ok(new { status = true, message = "Category inserted successfully." });
                        }
                        else
                        {
                            return BadRequest(new { status = false, message = "Category not inserted." });
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

        #region Update category

        [HttpPut("UpdateCategory")]
        public async Task<IActionResult> UpdateCategory([FromForm] CategoryModel category)
        {
            try
            {
                string iconImagePath = category.IconImage ?? string.Empty;
                string thumbnailImagePath = category.ThumbnailImage ?? string.Empty;

                // Retrieve from DB if missing
                if ((string.IsNullOrEmpty(iconImagePath) && (category.IconImage1 == null || category.IconImage1.Length == 0)) ||
                    (string.IsNullOrEmpty(thumbnailImagePath) && (category.ThumbnailImage1 == null || category.ThumbnailImage1.Length == 0)))
                {
                    using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                    using (SqlCommand cmd = new SqlCommand("sp_GetCategoryImagesByID", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@CategoryID", category.CategoryID);
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
                if (category.IconImage1 != null && category.IconImage1.Length > 0)
                {
                    var iconFolderPath = Path.Combine(_env.WebRootPath, "Images", "Category");
                    if (!Directory.Exists(iconFolderPath))
                        Directory.CreateDirectory(iconFolderPath);

                    var originalFileName = Path.GetFileNameWithoutExtension(category.IconImage1.FileName);
                    var fileExt = Path.GetExtension(category.IconImage1.FileName);
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
                        await category.IconImage1.CopyToAsync(stream);
                    }

                    iconImagePath = Path.Combine("Category", uniqueFileName).Replace("\\", "/");
                }

                // Handle ThumbnailImage1 upload
                if (category.ThumbnailImage1 != null && category.ThumbnailImage1.Length > 0)
                {
                    var thumbFolderPath = Path.Combine(_env.WebRootPath, "Images", "Category");
                    if (!Directory.Exists(thumbFolderPath))
                        Directory.CreateDirectory(thumbFolderPath);

                    var originalFileName = Path.GetFileNameWithoutExtension(category.ThumbnailImage1.FileName);
                    var fileExt = Path.GetExtension(category.ThumbnailImage1.FileName);
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
                        await category.ThumbnailImage1.CopyToAsync(stream);
                    }

                    thumbnailImagePath = Path.Combine("Category", uniqueFileName).Replace("\\", "/");
                }


                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_UpdateServiceCategory", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@CategoryID", category.CategoryID);
                        cmd.Parameters.AddWithValue("@CategoryName", category.CategoryName);
                        cmd.Parameters.AddWithValue("@Description", category.Description);
                        cmd.Parameters.AddWithValue("@IsActive", category.IsActive);
                        cmd.Parameters.AddWithValue("@ModifiedBy", category.ModifiedBy);
                        cmd.Parameters.AddWithValue("@IconImage", iconImagePath);
                        cmd.Parameters.AddWithValue("@ThumbnailImage", thumbnailImagePath);
                        conn.Open();
                        int rows = cmd.ExecuteNonQuery();

                        if (rows > 0)
                        {
                            return Ok(new { status = true, message = "Category updated successfully." });
                        }
                        else
                        {
                            return BadRequest(new { status = false, message = "Category not updated." });
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



        #region GetListCategory

        [HttpGet]

        public IActionResult GetListCategory()
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetListAllCategories", conn))
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


        #region CategoryById


        [HttpGet("categoryid")]

        public IActionResult GetCategoryById(int categoryid)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetListAllCategoriesByID", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@CategoryID", categoryid);
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
                    return NotFound(new { message = "Category not found" });
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
                return StatusCode(500, new { message = "An error occurred while retrieving the Categories.", error = ex.Message });

            }

        }

        #endregion


        #region DeleteCategory
        [HttpDelete("categoryid")]

        public IActionResult DeleteCategory(int categoryid)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_DeleteServiceCategory", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@CategoryID", categoryid);
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
