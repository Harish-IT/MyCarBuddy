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
                    using (SqlCommand cmd = new SqlCommand("InsertServiceSubCategories1", conn))
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
                            return Ok(new { status = true, message = " SubCategory inserted successfully." });
                        }
                        else
                        {
                            return BadRequest(new { status = false, message = "SubCategory not inserted." });
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



    }
}
