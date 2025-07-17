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
using System.Threading.Tasks;

namespace MyCarBuddy.API.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SubCategory1Controller : ControllerBase
    {

        private readonly IConfiguration _configuration;
        private readonly ILogger<SubCategory1Controller> _logger;
        private readonly IWebHostEnvironment _env;


        public SubCategory1Controller(IConfiguration configuration, ILogger<SubCategory1Controller> logger, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _logger = logger;
            _env = env;
        }

        #region Insert Category

        [HttpPost("AddSubCategory1")]
        public async Task<IActionResult> SubCategory([FromForm] SubCategoriesModel1 subcategory)
        {
            try
            {
                string iconImagePath = string.Empty;
                string thumbnailImagePath = string.Empty;

                if (subcategory.IconImage1 != null && subcategory.IconImage1.Length > 0)
                {
                    var iconFolderPath = Path.Combine(_env.WebRootPath, "Images", "SubCategory1");
                    if (!Directory.Exists(iconFolderPath))
                        Directory.CreateDirectory(iconFolderPath);

                    var iconFileName = Path.GetFileName(subcategory.IconImage1.FileName);
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
                        await subcategory.IconImage1.CopyToAsync(stream);
                    }

                    iconImagePath = Path.Combine("SubCategory1", iconFileName).Replace("\\", "/");
                }

                if (subcategory.ThumbnailImage1 != null && subcategory.ThumbnailImage1.Length > 0)
                {
                    var thumbFolderPath = Path.Combine(_env.WebRootPath, "Images", "SubCategory1");
                    if (!Directory.Exists(thumbFolderPath))
                        Directory.CreateDirectory(thumbFolderPath);

                    var thumbFileName = Path.GetFileName(subcategory.ThumbnailImage1.FileName);
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
                        await subcategory.ThumbnailImage1.CopyToAsync(stream);
                    }

                    thumbnailImagePath = Path.Combine("SubCategory1", thumbFileName).Replace("\\", "/");
                }


                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_InsertServiceSubCategories1", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@CategoryID", subcategory.CategoryID);
                        cmd.Parameters.AddWithValue("SubCategoryName", subcategory.SubCategoryName);
                        cmd.Parameters.AddWithValue("@Description", subcategory.Description ?? "");

                        cmd.Parameters.AddWithValue("@IconImage", iconImagePath);
                        cmd.Parameters.AddWithValue("@ThumbnailImage", thumbnailImagePath);
                        cmd.Parameters.AddWithValue("@CreatedBy", subcategory.CreatedBy ?? 0);

                        conn.Open();
                        int rows = cmd.ExecuteNonQuery();

                        if (rows > 0)
                        {
                            return Ok(new { status = true, message = " SubCategory-1 inserted successfully." });
                        }
                        else
                        {
                            return BadRequest(new { status = false, message = "SubCategory-1 not inserted." });
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


        #region Update sub category 1


        [HttpPut("UpdateSubCategory1")]
        public async Task<IActionResult> UpdateSubCategory1([FromForm] SubCategoriesModel1 subcategory1)
        {
            try
            {
                string iconImagePath = subcategory1.IconImage ?? string.Empty;
                string thumbnailImagePath = subcategory1.ThumbnailImage ?? string.Empty;


                if ((string.IsNullOrEmpty(iconImagePath) && (subcategory1.IconImage1 == null || subcategory1.IconImage1.Length == 0)) ||
                 (string.IsNullOrEmpty(thumbnailImagePath) && (subcategory1.ThumbnailImage1 == null || subcategory1.ThumbnailImage1.Length == 0)))
                {
                    using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                    using (SqlCommand cmd = new SqlCommand("sp_GetSubCategory1ImagesByID", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@SubCategoryID", subcategory1.SubCategoryID);
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


                if (subcategory1.IconImage1 != null && subcategory1.IconImage1.Length > 0)
                {
                    var iconFolderPath = Path.Combine(_env.WebRootPath, "Images", "SubCategory1");
                    if (!Directory.Exists(iconFolderPath))
                        Directory.CreateDirectory(iconFolderPath);

                    var iconFileName = Path.GetFileName(subcategory1.IconImage1.FileName);
                    var iconFullPath = Path.Combine(iconFolderPath, iconFileName);

                    int counter = 1;
                    while (System.IO.File.Exists(iconFullPath))
                    {
                        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(iconFileName);
                        var ext = Path.GetExtension(iconFileName);
                        iconFileName = $"{fileNameWithoutExt}_{counter}{ext}";
                        iconFullPath = Path.Combine(iconFolderPath, iconFileName);
                        counter++;
                    }

                    using (var stream = new FileStream(iconFullPath, FileMode.Create))
                    {
                        await subcategory1.IconImage1.CopyToAsync(stream);
                    }

                    iconImagePath = Path.Combine("Images", "SubCategory1", iconFileName).Replace("\\", "/");
                }

                if (subcategory1.ThumbnailImage1 != null && subcategory1.ThumbnailImage1.Length > 0)
                {
                    var thumbFolderPath = Path.Combine(_env.WebRootPath, "Images", "SubCategory1");
                    if (!Directory.Exists(thumbFolderPath))
                        Directory.CreateDirectory(thumbFolderPath);

                    var thumbFileName = Path.GetFileName(subcategory1.ThumbnailImage1.FileName);
                    var thumbFullPath = Path.Combine(thumbFolderPath, thumbFileName);

                    int counter = 1;
                    while (System.IO.File.Exists(thumbFullPath))
                    {
                        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(thumbFileName);
                        var ext = Path.GetExtension(thumbFileName);
                        thumbFileName = $"{fileNameWithoutExt}_{counter}{ext}";
                        thumbFullPath = Path.Combine(thumbFolderPath, thumbFileName);
                        counter++;
                    }

                    using (var stream = new FileStream(thumbFullPath, FileMode.Create))
                    {
                        await subcategory1.ThumbnailImage1.CopyToAsync(stream);
                    }

                    thumbnailImagePath = Path.Combine("Images", "SubCategory1", thumbFileName).Replace("\\", "/");
                }
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_UpdateServiceSubCategories1", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@SubCategoryID", subcategory1.SubCategoryID);
                        cmd.Parameters.AddWithValue("@CategoryID", subcategory1.CategoryID);
                        cmd.Parameters.AddWithValue("@SubCategoryName", subcategory1.SubCategoryName);
                        cmd.Parameters.AddWithValue("@Description", subcategory1.Description);

                    
                        cmd.Parameters.AddWithValue("@IconImage", iconImagePath);
                        cmd.Parameters.AddWithValue("@ThumbnailImage", thumbnailImagePath);
                        cmd.Parameters.AddWithValue("@ModifiedBy", subcategory1.ModifiedBy);
                        conn.Open();
                        int rows = cmd.ExecuteNonQuery();

                        if (rows > 0)
                        {
                            return Ok(new { status = true, message = " Sub Category-1 updated successfully." });
                        }
                        else
                        {
                            return BadRequest(new { status = false, message = "Sub Category-1 not updated." });
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



        #region GetListSubCategory1

        [HttpGet]

        public IActionResult GetListSubCategory1()
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetListServiceSubCategories1", conn))
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


        [HttpGet("subcategoryid1")]

        public IActionResult GetSubCategory1ById(int subcategoryid1)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetServiceSubCategories1ByID", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@SubCategoryID", subcategoryid1);
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
                    return NotFound(new { message = "Sub Category-1 not found.." });
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
                return StatusCode(500, new { message = "An error occurred while retrieving the Sub Categories-1.", error = ex.Message });

            }

        }

        #endregion



        #region DeleteCategory

        [HttpDelete("subcategoryid1")]

        public IActionResult DeleteSubCategory1(int subcategoryid1)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_DeleteServiceSubCategories1ByID", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@SubCategoryID", subcategoryid1);
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
