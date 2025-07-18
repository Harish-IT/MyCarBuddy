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
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class FuelTypesController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<FuelTypesController> _logger;
        private readonly IWebHostEnvironment _env;

        public FuelTypesController(IConfiguration configuration, ILogger<FuelTypesController> logger,IWebHostEnvironment env)
        {
            _configuration = configuration;
            _logger = logger;
            _env = env;
        }

        private string GetRandomAlphanumericString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }




        [HttpPost("InsertFuelType")]

        public async Task<IActionResult> InsertFuelType([FromForm] FuelTypeModel fueltype)
        {
            try
            {
                var missingFields = new List<string>();
                if (string.IsNullOrWhiteSpace(fueltype.FuelTypeName))
                    missingFields.Add("FuelTypeName");

                if (missingFields.Any())
                {
                    return BadRequest(new { status = false, message = $"The following fields are required: {string.Join(", ", missingFields)}" });
                }


                //string brandLogoFileName = null;
                //if (fueltype.FuelImage1 != null && fueltype.FuelImage1.Length > 0)
                //{

                //    var brandLogoFolder = Path.Combine(_env.WebRootPath, "Images", "FuelImages");

                //    if (!Directory.Exists(brandLogoFolder))
                //        Directory.CreateDirectory(brandLogoFolder);

                //    var originalFileName = Path.GetFileName(fueltype.FuelImage1.FileName);
                //    var filePath = Path.Combine(brandLogoFolder, originalFileName);

                //    if (System.IO.File.Exists(filePath))
                //    {
                //        var uniqueFileName = $"{Guid.NewGuid()}_{originalFileName}";
                //        filePath = Path.Combine(brandLogoFolder, uniqueFileName);
                //        //brandLogoFileName = "/FuelImages/{uniqueFileName}";
                //        brandLogoFileName = $"FuelImages/{uniqueFileName}";
                //    }
                //    else
                //    {
                //        brandLogoFileName = $"FuelImages/{originalFileName}";
                //    }

                //    using (var stream = new FileStream(filePath, FileMode.Create))
                //    {
                //        await fueltype.FuelImage1.CopyToAsync(stream);
                //    }
                //}

                string brandLogoFileName = null;
                if (fueltype.FuelImage1 != null && fueltype.FuelImage1.Length > 0)
                {
                    var brandLogoFolder = Path.Combine(_env.WebRootPath, "Images", "FuelImages");

                    if (!Directory.Exists(brandLogoFolder))
                        Directory.CreateDirectory(brandLogoFolder);

                    var originalFileName = Path.GetFileNameWithoutExtension(fueltype.FuelImage1.FileName);
                    var fileExt = Path.GetExtension(fueltype.FuelImage1.FileName);
                    var randomString = GetRandomAlphanumericString(8); // 8-character random string
                    string uniqueFileName = $"{originalFileName}_{randomString}{fileExt}";
                    var filePath = Path.Combine(brandLogoFolder, uniqueFileName);

                    // Optional: Extra collision check (very rare with random string)
                    int counter = 1;
                    while (System.IO.File.Exists(filePath))
                    {
                        uniqueFileName = $"{originalFileName}_{randomString}_{counter}{fileExt}";
                        filePath = Path.Combine(brandLogoFolder, uniqueFileName);
                        counter++;
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await fueltype.FuelImage1.CopyToAsync(stream);
                    }

                    // This is the path you can store in the database
                    brandLogoFileName = $"FuelImages/{uniqueFileName}";
                }

                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_InsertFuelTypes", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@FuelTypeName", fueltype.FuelTypeName);
                        cmd.Parameters.AddWithValue("@FuelImage", (object?)brandLogoFileName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CreatedBy", fueltype.CreatedBy);
                        cmd.Parameters.AddWithValue("@IsActive", fueltype.IsActive);

                        await conn.OpenAsync();
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return Ok(new { status = true, message = "Fuel type inserted successfully." });


            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("Fuel type  already exists"))
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




        [HttpPut("UpdateFuelType")]
        public async Task<IActionResult> UpdateFuelType([FromForm] FuelTypeModel fueltype)
        {
            try
            {
                var missingFields = new List<string>();
                if (fueltype.FuelTypeID == null || fueltype.FuelTypeID <= 0)
                    missingFields.Add("FuelTypeID");
                if (string.IsNullOrWhiteSpace(fueltype.FuelTypeName))
                    missingFields.Add("FuelTypeName");
                if (fueltype.ModifiedBy == null)
                    missingFields.Add("ModifiedBy");
                if (fueltype.Status == null)
                    missingFields.Add("Status");

                if (missingFields.Any())
                {
                    return BadRequest(new { status = false, message = $"The following fields are required: {string.Join(", ", missingFields)}" });
                }

                string fuelImagePath = fueltype.FuelImage;

                // Retrieve from DB if missing and no new file uploaded
                if ((fueltype.FuelImage1 == null || fueltype.FuelImage1.Length == 0) && string.IsNullOrEmpty(fuelImagePath))
                {
                    using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                    using (SqlCommand cmd = new SqlCommand("sp_GetFuelImageByID", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@FuelTypeID", fueltype.FuelTypeID);

                        await conn.OpenAsync();
                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null)
                        {
                            fuelImagePath = result.ToString();
                        }
                    }
                }

                // Handle FuelImage1 upload
                if (fueltype.FuelImage1 != null && fueltype.FuelImage1.Length > 0)
                {
                    var fuelImageFolder = Path.Combine(_env.WebRootPath, "Images", "FuelImages");
                    if (!Directory.Exists(fuelImageFolder))
                        Directory.CreateDirectory(fuelImageFolder);

                    var originalFileName = Path.GetFileNameWithoutExtension(fueltype.FuelImage1.FileName);
                    var fileExt = Path.GetExtension(fueltype.FuelImage1.FileName);
                    var randomString = GetRandomAlphanumericString(8); // 8-character random string
                    string uniqueFileName = $"{originalFileName}_{randomString}{fileExt}";
                    var filePath = Path.Combine(fuelImageFolder, uniqueFileName);

                    // Optional: Extra collision check (very rare with random string)
                    int counter = 1;
                    while (System.IO.File.Exists(filePath))
                    {
                        uniqueFileName = $"{originalFileName}_{randomString}_{counter}{fileExt}";
                        filePath = Path.Combine(fuelImageFolder, uniqueFileName);
                        counter++;
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await fueltype.FuelImage1.CopyToAsync(stream);
                    }

                    // This is the path you can store in the database
                    fuelImagePath = $"FuelImages/{uniqueFileName}";
                }

                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_UpdateFuelTypes", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@FuelTypeID", fueltype.FuelTypeID);
                        cmd.Parameters.AddWithValue("@FuelTypeName", fueltype.FuelTypeName);
                        cmd.Parameters.AddWithValue("@FuelImage", (object?)fuelImagePath ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsActive", fueltype.IsActive);
                        cmd.Parameters.AddWithValue("@Status", fueltype.Status);
                        cmd.Parameters.AddWithValue("@ModifiedBy", fueltype.ModifiedBy);

                        await conn.OpenAsync();
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return Ok(new { status = true, message = "Fuel type updated successfully." });
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("already inactive") || ex.Message.Contains("already exists"))
                    return BadRequest(new { status = false, message = ex.Message });

                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { status = false, message = "An error occurred while updating the record.", error = ex.Message });
            }
            catch (Exception ex)
            {
                ErrorLogger.LogToDatabase(ex, HttpContext, _configuration, _logger);
                return StatusCode(500, new { status = false, message = "An error occurred while updating the record.", error = ex.Message });
            }
        }


        [HttpGet("GetFuelTypes")]

        public IActionResult GetAllFuelTypes()
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_ListAllFuelTypes", conn))
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
                        message = "Fuel Types  retrieved successfully.",
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


        [HttpGet("fueltypeid")]

        public IActionResult GetFuelTypesByID(int fueltypeid)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetFuelTypesById", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@FuelTypeID", fueltypeid);
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
                    return NotFound(new { message = "Fuel Types  not found" });
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
                return StatusCode(500, new { message = "An error occurred while retrieving the Fuel types.", error = ex.Message });
            }
        }



        [HttpDelete("fueltypeid")]
        public IActionResult DeleteVehicleBrand(int fueltypeid)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_DeleteFuelTypeByID", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@FuelTypeID", fueltypeid);
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



    }
}
