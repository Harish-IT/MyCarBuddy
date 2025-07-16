using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyCarBuddy.API.Models;
using MyCarBuddy.API.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MyCarBuddy.API.Controllers
{
    // [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class VehicleBrandsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<VehicleBrandsController> _logger;
        private readonly IWebHostEnvironment _env;

        public VehicleBrandsController(IConfiguration configuration, ILogger<VehicleBrandsController> logger,IWebHostEnvironment env)
        {
            _configuration = configuration;
            _logger = logger;
            _env = env;
        }

        [HttpPost("InsertVehicleBrand")]

        public async Task<IActionResult> InsertVehicleBrand([FromForm] VehicleBrandsModel vehiclebrands)
        {
            try
            {
                // Validate required fields
                var missingFields = new List<string>();
                if (string.IsNullOrWhiteSpace(vehiclebrands.BrandName))
                    missingFields.Add("BrandName");

                if (missingFields.Any())
                {
                    return BadRequest(new { status = false, message = $"The following fields are required: {string.Join(", ", missingFields)}" });
                }


                // Handle BrandLogo upload (save with original name, ensure uniqueness)
                string brandLogoFileName = null;
                if (vehiclebrands.BrandLogoImage != null && vehiclebrands.BrandLogoImage.Length > 0)
                {
                    // Set the subfolder path
                    var brandLogoFolder = Path.Combine(_env.WebRootPath, "Images", "BrandLogo");
                    if (!Directory.Exists(brandLogoFolder))
                        Directory.CreateDirectory(brandLogoFolder);

                    var originalFileName = Path.GetFileName(vehiclebrands.BrandLogoImage.FileName);
                    var filePath = Path.Combine(brandLogoFolder, originalFileName);

                    // Ensure uniqueness
                    if (System.IO.File.Exists(filePath))
                    {
                        var uniqueFileName = $"{Guid.NewGuid()}_{originalFileName}";
                        filePath = Path.Combine(brandLogoFolder, uniqueFileName);
                        brandLogoFileName = $"/BrandLogo/{uniqueFileName}";
                    }
                    else
                    {
                        brandLogoFileName = $"/BrandLogo/{originalFileName}";
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await vehiclebrands.BrandLogoImage.CopyToAsync(stream);
                    }
                }
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_InsertVehicleBrands", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@BrandName", vehiclebrands.BrandName);
                        cmd.Parameters.AddWithValue("@BrandLogo", (object?)brandLogoFileName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CreatedBy", vehiclebrands.CreatedBy);

                        await conn.OpenAsync();
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return Ok(new { status = true, message = "Brand inserted successfully." });


            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("Brand name already exists"))
                    return BadRequest(new { status = false, message = ex.Message });

                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { status = false, message = "An error occurred while inserting the record.", error = ex.Message });
            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { status = false, message = "An error occurred while inserting the record.", error = ex.Message });
            }


        }
        [HttpPut("UpdateVehicleBrand")]

        public async Task<IActionResult> UpdateVehicleBrand([FromForm] VehicleBrandsModel vehiclebrands)
        {
            try
            {
                // Validate required fields
                var missingFields = new List<string>();
                if (vehiclebrands.BrandID <= 0)
                    missingFields.Add("BrandID");
                if (string.IsNullOrWhiteSpace(vehiclebrands.BrandName))
                    missingFields.Add("BrandName");
                if (vehiclebrands.ModifiedBy == null)
                    missingFields.Add("ModifiedBy");

                if (missingFields.Any())
                {
                    return BadRequest(new { status = false, message = $"The following fields are required: {string.Join(", ", missingFields)}" });
                }

                string brandLogoFileName = vehiclebrands.BrandLogo; 
                if (vehiclebrands.BrandLogoImage != null && vehiclebrands.BrandLogoImage.Length > 0)
                {
                    var brandLogoFolder = Path.Combine(_env.WebRootPath, "Images", "BrandLogo");
                    if (!Directory.Exists(brandLogoFolder))
                        Directory.CreateDirectory(brandLogoFolder);

                    var originalFileName = Path.GetFileName(vehiclebrands.BrandLogoImage.FileName);
                    var filePath = Path.Combine(brandLogoFolder, originalFileName);

                    if (System.IO.File.Exists(filePath))
                    {
                        var uniqueFileName = $"{Guid.NewGuid()}_{originalFileName}";
                        filePath = Path.Combine(brandLogoFolder, uniqueFileName);
                        brandLogoFileName = $"/BrandLogo/{uniqueFileName}";
                    }
                    else
                    {
                        brandLogoFileName = $"/BrandLogo/{originalFileName}";
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await vehiclebrands.BrandLogoImage.CopyToAsync(stream);
                    }
                }

                using(SqlConnection conn=new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using(SqlCommand cmd=new SqlCommand("sp_UpdateVehicleBrands",conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@BrandID", vehiclebrands.BrandID);
                        cmd.Parameters.AddWithValue("@BrandName", vehiclebrands.BrandName);
                        cmd.Parameters.AddWithValue("@BrandLogo", (object?)brandLogoFileName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ModifiedBy", vehiclebrands.ModifiedBy);
                        cmd.Parameters.AddWithValue("@IsActive", vehiclebrands.IsActive);
                        cmd.Parameters.AddWithValue("@Status", vehiclebrands.Status);

                        await conn.OpenAsync();
                        await cmd.ExecuteNonQueryAsync();


                    }
                }

                return Ok(new { status = true, message = "Brand updated successfully." });


            }
            catch (SqlException ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { status = false, message = "An error occurred while updating the record.", error = ex.Message });
            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { status = false, message = "An error occurred while updating the record.", error = ex.Message });
            }
        }

        [HttpGet("GetVehicleBrands")]

        public IActionResult GetAllVehicleBrands()
        {
            try
            {
                DataTable dt = new DataTable();
                using(SqlConnection conn=new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using(SqlCommand cmd=new SqlCommand("sp_ListAllVehicleBrands",conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            dt.Load(reader);
                        }
                        conn.Close();
                    }
                    // Convert DataTable to JSON-friendly list
                    var jsonList = new List<Dictionary<string, object>>();
                    foreach (DataRow row in dt.Rows)
                    {
                        var dict = new Dictionary<string, object>();
                        foreach (DataColumn col in dt.Columns)
                        {
                            dict[col.ColumnName] = row[col] is DBNull ? null : row[col];
                        }
                        jsonList.Add(dict);
                    }

                    return Ok(new
                    {
                        status = true,
                        message = "Technicians with documents retrieved successfully.",
                        data = jsonList
                    });
                }

            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { status = false, message = "An error occurred while fetching the records.", error = ex.Message });
            }
        }

        [HttpDelete("brandid")]
        public IActionResult DeleteVehicleBrand(int brandid)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_DeleteVehicleBrands", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@BrandID", brandid);
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int success = Convert.ToInt32(reader["Success"]);
                                string message = reader["Message"].ToString();

                                if (success == 1)
                                    return Ok(new { status = true, message });
                                else
                                    return NotFound(new { status = false, message });
                            }
                            else
                            {
                                return StatusCode(500, new { status = false, message = "No response from database." });
                            }
                        }


                    }

                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while deleting the record.", error = ex.Message });

            }
        }


        [HttpGet("vehiclebrandid")]

        public IActionResult GetVehicleBrandsByID(int vehiclebrandid)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetVehicleBrandsById", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@BrandID", vehiclebrandid);
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            dt.Load(reader); // Load all columns and rows
                        }
                        conn.Close();
                    }
                }

                if (dt.Rows.Count == 0)
                {
                    return NotFound(new { message = "vehicle brand not found" });
                }

                // Convert DataTable to JSON-friendly structure
                var jsonResult = new List<Dictionary<string, object>>();
                foreach (DataRow row in dt.Rows)
                {
                    var dict = new Dictionary<string, object>();
                    foreach (DataColumn col in dt.Columns)
                    {
                        dict[col.ColumnName] = row[col];
                    }
                    jsonResult.Add(dict);
                }

                // If you expect only one row, you can return jsonResult[0]
                return Ok(jsonResult.Count == 1 ? jsonResult[0] : jsonResult);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { message = "An error occurred while retrieving the vehicle brand.", error = ex.Message });
            }
        }


    }
}
